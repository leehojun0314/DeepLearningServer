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

    /// <summary>
    /// 파이썬 훈련 서버 URL (예: http://localhost:8000)
    /// </summary>
    public string PyTrainingServerUrl { get; set; } = "http://localhost:8000";

    /// <summary>
    /// true면 파이썬 서버로 훈련 수행, false면 Euresys 사용
    /// </summary>
    public bool UsePythonServer { get; set; } = false;

    /// <summary>
    /// 파이썬 훈련 서버에서 사용할 데이터 경로
    /// </summary>
    public string PyTrainingDataPath { get; set; } = "";

    /// <summary>
    /// 파이썬 훈련 서버에서 모델을 저장할 출력 경로
    /// </summary>
    public string PyTrainingOutputPath { get; set; } = "";
}