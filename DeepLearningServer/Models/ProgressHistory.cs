using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DeepLearningServer.Models
{
    public class ProgressHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public bool IsTraining { get; set; }
        public double Progress { get; set; }
        public double BestIteration { get; set; }
        public string LearningRateParameters { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // 외래 키 설정
        public int TrainingRecordId { get; set; }
        public TrainingRecordModel TrainingRecord { get; set; }
    }
}
