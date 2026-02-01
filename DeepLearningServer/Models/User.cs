using System.ComponentModel.DataAnnotations;

namespace DeepLearningServer.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Username { get; set; } = null!;

        [DataType(DataType.Password)]
        public string PasswordHash { get; set; } = null!;

        [MaxLength(100)]
        public string? Email { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<PwdResetRequest> PwdResetRequests { get; set; } = new List<PwdResetRequest>();
    }
}
