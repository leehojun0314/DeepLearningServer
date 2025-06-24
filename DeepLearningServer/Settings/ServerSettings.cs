namespace DeepLearningServer.Settings;

public class ServerSettings
{
    public required string LoggingLevel { get; set; }
    public required string MiddleImagePath { get; set; }
    public required string LargeImagePath { get; set; }
    public required string ModelDirectory { get; set; }
    public required string EvaluationModelDirectory { get; set; }
    public required int PORT { get; set; }
    public required string TempImageDirectory { get; set; }
    public int EarlyStoppingPatience { get; set; } = 5;
    public float EarlyStoppingThreshold { get; set; } = 0.001f;
}