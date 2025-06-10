namespace DeepLearningServer.Dtos
{
    /// <summary>
    /// 저장된 모델 파일의 정보를 나타내는 DTO 클래스입니다.
    /// </summary>
    public class ModelInfoDto
    {
        /// <summary>
        /// 모델 파일명 (예: "123.edltool")
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 전체 파일 경로
        /// </summary>
        public string FullPath { get; set; } = string.Empty;

        /// <summary>
        /// 상대 경로 (ModelDirectory 기준)
        /// </summary>
        public string RelativePath { get; set; } = string.Empty;

        /// <summary>
        /// 이미지 크기 (LARGE, MIDDLE)
        /// </summary>
        public string Size { get; set; } = string.Empty;

        /// <summary>
        /// 모델 타입 (BASE, Release, EVALUATION)
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// ADMS 이름
        /// </summary>
        public string AdmsName { get; set; } = string.Empty;

        /// <summary>
        /// 프로세스 ID (파일명에서 추출)
        /// </summary>
        public string ProcessId { get; set; } = string.Empty;

        /// <summary>
        /// 파일 크기 (바이트)
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// 파일 크기 (읽기 쉬운 형태)
        /// </summary>
        public string FileSizeFormatted { get; set; } = string.Empty;

        /// <summary>
        /// 파일 생성일시
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// 파일 수정일시
        /// </summary>
        public DateTime ModifiedDate { get; set; }
    }

    /// <summary>
    /// 모델 조회 요청 매개변수를 나타내는 DTO 클래스입니다.
    /// </summary>
    public class ModelListRequestDto
    {
        /// <summary>
        /// 이미지 크기 필터 (LARGE, MIDDLE, 또는 전체)
        /// </summary>
        public string? Size { get; set; }

        /// <summary>
        /// 모델 타입 필터 (BASE, Release, EVALUATION, 또는 전체)
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// ADMS 이름 필터 (특정 ADMS만 조회)
        /// </summary>
        public string? AdmsName { get; set; }

        /// <summary>
        /// 프로세스 ID 필터 (특정 프로세스만 조회)
        /// </summary>
        public string? ProcessId { get; set; }
    }
} 