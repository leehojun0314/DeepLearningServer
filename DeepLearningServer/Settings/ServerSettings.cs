namespace DeepLearningServer.Settings;

public class ServerSettings
{
    public required string LoggingLevel { get; set; }
    public required string MiddleImagePath { get; set; }
    public required string LargeImagePath { get; set; }
    public required string ModelDirectory { get; set; }
    public required string EvaluationModelDirectory { get; set; }
    public required int PORT { get; set; }
}