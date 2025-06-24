namespace DeepLearningServer.Enums;

public enum TrainingStatus
{
    Loading,   // 이미지 로딩 중
    Running,   // 실제 훈련 중
    Completed, // 훈련 완료
    Failed,    // 훈련 실패
    Stanby     // 대기 상태
}