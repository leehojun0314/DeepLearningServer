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
    /// 훈련이 끝난 뒤 학습된 모델을 해당 ADMS 의 LocalIp 로 자동 전송할지 여부.
    /// 기본값 false — 모델은 서버의 EvaluationModelDirectory 에만 저장되고,
    /// 클라이언트로 보낼 때는 POST /api/model/send-remote 를 사용합니다.
    /// </summary>
    public bool AutoUploadModelToClient { get; set; } = false;

}