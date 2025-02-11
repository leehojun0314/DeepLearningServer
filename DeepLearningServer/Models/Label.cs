using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DeepLearningServer.Models
{
    public class Label
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Key { get; set; } = string.Empty;
        public float Value { get; set; }

        // 외래 키 설정
        public int TrainingRecordId { get; set; }
        public TrainingRecordModel TrainingRecord { get; set; }
    }
}
