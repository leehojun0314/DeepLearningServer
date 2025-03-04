using System.ComponentModel.DataAnnotations;

namespace DeepLearningServer.Models
{
    public class ModelRecord
    {
        public int Id { get; set; }

        [Required]
        public string ModelName { get; set; } = null!;

        [Required]
        public string Status { get; set; } = "saved"; // "deployed", "saved", "error" 등 상태 관리

        public string? ClientPath { get; set; } // 클라이언트 저장 경로
        public string? ServerPath { get; set; } // 딥러닝 서버 내부 저장 경로

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // 🔹 AdmsProcessType을 참조 (라지/미들 모델 구분)
        [Required]
        public int AdmsProcessTypeId { get; set; }
        public virtual AdmsProcessType AdmsProcessType { get; set; } = null!;

        // 🔹 TrainingRecord를 참조 (훈련 기록과 연결)
        public int? TrainingRecordId { get; set; }
        public virtual TrainingRecord? TrainingRecord { get; set; }
    }
}
