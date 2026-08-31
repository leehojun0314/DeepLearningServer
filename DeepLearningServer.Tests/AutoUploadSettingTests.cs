using DeepLearningServer.Settings;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DeepLearningServer.Tests;

/// <summary>
/// The customer asked for the "send the trained model to the client automatically"
/// behaviour to be removed. It is now gated by ServerSettings.AutoUploadModelToClient,
/// which must default to OFF - both in code and in the shipped appsettings.json -
/// so an upgrade never starts pushing models again on its own.
/// </summary>
public class AutoUploadSettingTests
{
    private static ServerSettings NewSettings() => new()
    {
        LoggingLevel = "Info",
        MiddleImagePath = "Z:\\M",
        LargeImagePath = "Z:\\L",
        ModelDirectory = "D:\\ADMS\\Transfer",
        EvaluationModelDirectory = "D:\\ADMS\\Transfer",
        PORT = 8082,
        TempImageDirectory = "D:\\ADMS\\Temp",
    };

    [Fact]
    public void AutoUpload_DefaultsToOff()
    {
        Assert.False(NewSettings().AutoUploadModelToClient);
    }

    [Fact]
    public void AutoUpload_CanStillBeTurnedBackOn()
    {
        var settings = NewSettings();
        settings.AutoUploadModelToClient = true;
        Assert.True(settings.AutoUploadModelToClient);
    }

    [Fact]
    public void ExistingDeployment_WithoutTheKey_HasAutoUploadOff()
    {
        // appsettings.json is not in the repo (it holds credentials), so upgraded
        // installations will have a ServerSettings section that predates this key.
        // Binding must leave the push disabled rather than defaulting it on.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServerSettings:LoggingLevel"] = "Info",
                ["ServerSettings:MiddleImagePath"] = "Z:\\M",
                ["ServerSettings:LargeImagePath"] = "Z:\\L",
                ["ServerSettings:ModelDirectory"] = "D:\\ADMS\\Transfer",
                ["ServerSettings:EvaluationModelDirectory"] = "D:\\ADMS\\Transfer",
                ["ServerSettings:PORT"] = "8082",
                ["ServerSettings:TempImageDirectory"] = "D:\\ADMS\\Temp",
                ["ServerSettings:UsePythonServer"] = "true",
            })
            .Build();

        var settings = config.GetSection("ServerSettings").Get<ServerSettings>();

        Assert.NotNull(settings);
        Assert.False(settings!.AutoUploadModelToClient);
    }

    [Fact]
    public void Deployment_CanOptBackIn_ViaConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServerSettings:LoggingLevel"] = "Info",
                ["ServerSettings:MiddleImagePath"] = "Z:\\M",
                ["ServerSettings:LargeImagePath"] = "Z:\\L",
                ["ServerSettings:ModelDirectory"] = "D:\\ADMS\\Transfer",
                ["ServerSettings:EvaluationModelDirectory"] = "D:\\ADMS\\Transfer",
                ["ServerSettings:PORT"] = "8082",
                ["ServerSettings:TempImageDirectory"] = "D:\\ADMS\\Temp",
                ["ServerSettings:AutoUploadModelToClient"] = "true",
            })
            .Build();

        var settings = config.GetSection("ServerSettings").Get<ServerSettings>();

        Assert.NotNull(settings);
        Assert.True(settings!.AutoUploadModelToClient);
    }
}
