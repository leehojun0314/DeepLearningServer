namespace DeepLearningServer.Models
{
    public class PwdResetRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.Now;
        public bool IsUsed { get; set; } = false;
        public virtual User User { get; set; } = null!;
        
    }
}
