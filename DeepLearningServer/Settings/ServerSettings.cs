namespace DeepLearningServer.Settings;

public class ServerSettings
{
    public required string LoggingLevel { get; set; }
    public required string MiddleImagePath { get; set; }
    public required string LargeImagePath { get; set; }
    public required string PretrainedModelPath { get; set; }
    public required int PORT { get; set; }
}