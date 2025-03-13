using System.ComponentModel.DataAnnotations;

namespace DeepLearningServer.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Username { get; set; } = null!;

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = null!; // 암호화된 비밀번호

        [MaxLength(100)]
        public string? Email { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<PwdResetRequest> PwdResetRequests { get; set; } = new List<PwdResetRequest>();
    }
}
