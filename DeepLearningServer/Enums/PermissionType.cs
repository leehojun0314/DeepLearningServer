namespace DeepLearningServer.Enums
{
    public enum PermissionType
    {
        RunModel,         // 모델 실행 권한
        ViewLogs,         // 로그 보기 권한
        ManageUsers,      // 사용자 관리 권한
        DeployModel,      // 모델 배포 권한
        TrainModel        // 모델 훈련 권한
    }
}
