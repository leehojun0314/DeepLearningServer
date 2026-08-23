using DeepLearningServer.Classes;
using DeepLearningServer.Dtos;
using System.IO;
using Xunit;

namespace DeepLearningServer.Tests;

/// <summary>
/// Regression tests for the stop path of the Python training bridge.
///
/// Reported symptom (train_cls_server 1.2.8 + DL Server 2.2.6): a stop request
/// is answered, the run appears to keep going, and the next training request is
/// refused with "The tool is already running."
///
/// Two independent causes are covered here:
///   1. The Python server reports "terminated"/"killed" when it has to force-kill
///      the training worker. 2.2.6 did not count those as terminal, so the status
///      poll loop never exited and never released the tool-busy flag.
///   2. A stop arriving while images were still being copied was dropped, because
///      the cancellation source only existed once TrainAsync had started.
/// </summary>
public class TrainingAiHttpBridgeStopTests
{
    private static PyTrainStatusResponse Status(string status, bool stoppedByUser = false) =>
        new() { Status = status, StoppedByUser = stoppedByUser };

    // --- 1. terminal status vocabulary -------------------------------------

    [Theory]
    [InlineData("completed")]
    [InlineData("stopped")]
    [InlineData("failed")]
    [InlineData("terminated")] // force-killed worker (train_cls_server 1.2.7-1.2.8)
    [InlineData("killed")]     // force-killed worker, terminate() timed out
    [InlineData("cancelled")]
    [InlineData("STOPPED")]    // casing must not matter
    [InlineData("  terminated  ")]
    public void TerminalStatuses_EndThePollLoop(string status)
    {
        Assert.True(TrainingAiHttpBridge.IsTerminalStatus(Status(status)),
            $"'{status}' must end the poll loop, otherwise the tool stays flagged busy forever.");
    }

    [Theory]
    [InlineData("running")]
    [InlineData("starting")]
    [InlineData("finalizing")]
    [InlineData("stopping")]
    [InlineData("stop_requested")]
    public void NonTerminalStatuses_KeepPolling(string status)
    {
        Assert.False(TrainingAiHttpBridge.IsTerminalStatus(Status(status)));
    }

    [Fact]
    public void NullStatus_IsNotTerminal()
    {
        Assert.False(TrainingAiHttpBridge.IsTerminalStatus(null));
        Assert.False(TrainingAiHttpBridge.IsTerminalStatus(new PyTrainStatusResponse()));
    }

    [Theory]
    [InlineData("stopped")]
    [InlineData("cancelled")]
    [InlineData("terminated")]
    [InlineData("killed")]
    public void ForcedAndGracefulStops_CountAsCancelled(string status)
    {
        Assert.True(TrainingAiHttpBridge.IsCancelledStatus(Status(status)));
    }

    [Fact]
    public void StoppedByUserFlag_CountsAsCancelled_WhateverTheStatusText()
    {
        Assert.True(TrainingAiHttpBridge.IsCancelledStatus(Status("running", stoppedByUser: true)));
    }

    [Fact]
    public void CompletedRun_IsNotCancelled()
    {
        Assert.True(TrainingAiHttpBridge.IsTerminalStatus(Status("completed")));
        Assert.False(TrainingAiHttpBridge.IsCancelledStatus(Status("completed")));
    }

    // --- 2. a stop before training starts must not be lost ------------------

    [Fact]
    public void FreshBridge_HasNoPendingStop()
    {
        using var bridge = new TrainingAiHttpBridge("http://localhost:9");
        Assert.False(bridge.IsStopRequested);
        bridge.ThrowIfStopRequested(); // must not throw
    }

    [Fact]
    public void RequestStop_IsRemembered_BeforeTrainingEverStarts()
    {
        using var bridge = new TrainingAiHttpBridge("http://localhost:9");

        // A stop arriving during the image-copy phase, before TrainAsync runs.
        bridge.RequestStop();

        Assert.True(bridge.IsStopRequested);
        Assert.Throws<OperationCanceledException>(() => bridge.ThrowIfStopRequested());
    }

    [Fact]
    public void RequestStop_IsIdempotent()
    {
        using var bridge = new TrainingAiHttpBridge("http://localhost:9");
        bridge.RequestStop();
        bridge.RequestStop(); // must not throw
        Assert.True(bridge.IsStopRequested);
    }

    [Fact]
    public void Dispose_AfterStop_DoesNotThrow()
    {
        var bridge = new TrainingAiHttpBridge("http://localhost:9");
        bridge.RequestStop();
        bridge.Dispose();
    }

    [Fact]
    public async Task LoadImages_AfterStop_AbortsTheCopy()
    {
        // The image copy runs for minutes on a real dataset. A stop arriving in
        // that window used to be dropped, and training started right after the
        // caller had been told it stopped.
        var root = Path.Combine(Path.GetTempPath(), "dls_stop_" + Guid.NewGuid().ToString("N"));
        var source = Path.Combine(root, "images", "NG", "BASE", "SCRATCH");
        var temp = Path.Combine(root, "temp");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(temp);
        File.WriteAllBytes(Path.Combine(source, "a.png"), new byte[] { 1, 2, 3 });
        File.WriteAllBytes(Path.Combine(source, "b.png"), new byte[] { 4, 5, 6 });

        try
        {
            using var bridge = new TrainingAiHttpBridge("http://127.0.0.1:9");
            bridge.RequestStop();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                bridge.LoadImagesAsync(
                    new[] { "SCRATCH" },
                    new[] { "PROC1" },
                    Path.Combine(root, "images"),
                    temp));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public async Task LoadImages_WithoutStop_CopiesNormally()
    {
        var root = Path.Combine(Path.GetTempPath(), "dls_stop_" + Guid.NewGuid().ToString("N"));
        var source = Path.Combine(root, "images", "NG", "BASE", "SCRATCH");
        var temp = Path.Combine(root, "temp");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(temp);
        File.WriteAllBytes(Path.Combine(source, "a.png"), new byte[] { 1, 2, 3 });
        File.WriteAllBytes(Path.Combine(source, "b.png"), new byte[] { 4, 5, 6 });

        try
        {
            using var bridge = new TrainingAiHttpBridge("http://127.0.0.1:9");

            var count = await bridge.LoadImagesAsync(
                new[] { "SCRATCH" },
                new[] { "PROC1" },
                Path.Combine(root, "images"),
                temp);

            Assert.Equal(2, count);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
        }
    }
}
