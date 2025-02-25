using System.ComponentModel.DataAnnotations;

namespace DeepLearningServer.Models
{
    public class TrainingAdmsProcess
    {
        public int Id { get; set; }
        public int TrainingRecordId { get; set; }
        public TrainingRecord TrainingRecord { get; set; } = null!;

        public int AdmsProcessId { get; set; }
        public AdmsProcess AdmsProcess { get; set; } = null!;
    }

}
