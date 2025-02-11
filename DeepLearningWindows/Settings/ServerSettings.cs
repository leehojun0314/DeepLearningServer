namespace DeepLearningServer.Settings;

public class ServerSettings
{
    public required string UriPrefix { get; set; }
    public required string LoggingLevel { get; set; }
    public required string MiddleImagePath { get; set; }
    public required string LargeImagePath { get; set; }
}