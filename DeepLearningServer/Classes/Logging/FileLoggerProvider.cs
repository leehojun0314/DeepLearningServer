using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace DeepLearningServer.Classes.Logging
{
  public sealed class FileLoggerProvider : ILoggerProvider
  {
    private readonly string _logsDirectory;
    private readonly LogLevel _minimumLevel;
    private readonly object _sync = new object();
    private StreamWriter _writer;
    private DateTime _currentDate;
    private bool _disposed;

    public FileLoggerProvider(string logsDirectory, LogLevel minimumLevel = LogLevel.Information)
    {
      _logsDirectory = logsDirectory ?? throw new ArgumentNullException(nameof(logsDirectory));
      _minimumLevel = minimumLevel;
      Directory.CreateDirectory(_logsDirectory);
      _currentDate = DateTime.Now.Date;
      EnsureWriterLocked();
    }

    public ILogger CreateLogger(string categoryName)
    {
      return new FileLogger(this, categoryName);
    }

    internal bool IsEnabled(LogLevel level)
    {
      return level >= _minimumLevel;
    }

    internal void WriteLine(string line)
    {
      if (_disposed) return;

      lock (_sync)
      {
        if (_disposed) return;
        try
        {
          RotateIfNeededLocked();
          if (_writer == null) return; // opening failed; skip and retry on next call
          _writer.WriteLine(line);
          _writer.Flush();
        }
        catch (IOException ex)
        {
          // I/O error; dispose and null writer so future calls retry opening
          System.Diagnostics.Trace.TraceError($"FileLoggerProvider: IOException while writing log: {ex}");
          DisposeWriterSafely();
        }
        catch (ObjectDisposedException ex)
        {
          // Writer was disposed unexpectedly; null writer so future calls retry opening
          System.Diagnostics.Trace.TraceError($"FileLoggerProvider: ObjectDisposedException while writing log: {ex}");
          _writer = null;
        }
      }
    }

    private void EnsureWriterLocked()
    {
      if (_writer != null) return;
      var filePath = GetLogFilePath(_currentDate);
      FileStream? fileStream = null;
      try
      {
        fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        _writer = new StreamWriter(fileStream, new UTF8Encoding(false))
        {
          AutoFlush = true
        };
        fileStream = null; // Ownership transferred to StreamWriter; do not dispose separately
      }
      catch (IOException ex)
      {
        // I/O error; leave _writer as null, next writes will retry opening
        System.Diagnostics.Trace.TraceError($"FileLoggerProvider: IOException while opening log file '{filePath}': {ex}");
      }
      catch (UnauthorizedAccessException ex)
      {
        // Access denied; leave _writer as null, next writes will retry opening
        System.Diagnostics.Trace.TraceError($"FileLoggerProvider: UnauthorizedAccessException while opening log file '{filePath}': {ex}");
      }
      catch (ArgumentException ex)
      {
        // Invalid path; leave _writer as null, next writes will retry opening
        System.Diagnostics.Trace.TraceError($"FileLoggerProvider: ArgumentException while opening log file '{filePath}': {ex}");
      }
      finally
      {
        // Dispose FileStream only if StreamWriter was not successfully created
        fileStream?.Dispose();
      }
    }

    private void DisposeWriterSafely()
    {
      try { _writer?.Dispose(); }
      catch (IOException ex)
      {
        // I/O error during flush or close; non-critical, but record for diagnostics.
        System.Diagnostics.Trace.TraceError($"FileLoggerProvider: IOException while disposing writer: {ex}");
      }
      catch (ObjectDisposedException ex)
      {
        // Writer was already disposed; non-critical, but record for diagnostics.
        System.Diagnostics.Trace.TraceError($"FileLoggerProvider: ObjectDisposedException while disposing writer: {ex}");
      }
      _writer = null;
    }

    private void RotateIfNeededLocked()
    {
      var today = DateTime.Now.Date;
      if (today == _currentDate && _writer != null) return;

      DisposeWriterSafely();
      _currentDate = today;
      EnsureWriterLocked();
    }

    private string GetLogFilePath(DateTime date)
    {
      var fileName = $"app-{date:yyyy-MM-dd}.log";
      return Path.Combine(_logsDirectory, fileName);
    }

    public void Dispose()
    {
      if (_disposed) return;
      lock (_sync)
      {
        if (_disposed) return;
        _disposed = true;
        _writer?.Dispose();
        _writer = null;
      }
    }

    private sealed class FileLogger : ILogger
    {
      private readonly FileLoggerProvider _provider;
      private readonly string _categoryName;

      public FileLogger(FileLoggerProvider provider, string categoryName)
      {
        _provider = provider;
        _categoryName = categoryName;
      }

      public IDisposable BeginScope<TState>(TState state)
      {
        return NullScope.Instance;
      }

      public bool IsEnabled(LogLevel logLevel)
      {
        return _provider.IsEnabled(logLevel);
      }

      public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
      {
        if (!_provider.IsEnabled(logLevel)) return;
        if (formatter == null) return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var message = formatter(state, exception);
        var level = logLevel.ToString().ToUpperInvariant();

        var line = $"{timestamp} [{level}] {_categoryName} ({eventId.Id}) {message}";
        if (exception != null)
        {
          line += Environment.NewLine + exception;
        }

        _provider.WriteLine(line);
      }
    }

    private sealed class NullScope : IDisposable
    {
      public static readonly NullScope Instance = new NullScope();
      public void Dispose() { }
    }
  }
}


