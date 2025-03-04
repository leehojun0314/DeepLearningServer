namespace DeepLearningServer.Models
{
    public class AdmsProcessType
    {
        public int Id { get; set; }
        public int AdmsProcessId { get; set; } // 🔥 AdmsProcess와 연결 (FK)
        public string Type { get; set; } = null!; // Small, Middle, Large
        public DateTime? LastSyncDate { get; set; } // 🔥 개별 동기화 날짜
        public bool IsTrainned { get; set; } // 🔥 Middle, Large만 적용
        public bool IsCategorized { get; set; } // 🔥 모든 타입에 적용

        // 🔥 FK 관계
        public virtual AdmsProcess AdmsProcess { get; set; } = null!;
        public virtual ICollection<ModelRecord> ModelRecords { get; set; } = new List<ModelRecord>();

    }
}
