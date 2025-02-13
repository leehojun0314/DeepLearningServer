using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DeepLearningServer.Models
{
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column(TypeName= "nvarchar(100)")]
        public string Name { get; set; } = string.Empty;

        // 외래 키 설정
        public int TrainingRecordId { get; set; }
        public TrainingRecord TrainingRecord { get; set; }
    }
}
