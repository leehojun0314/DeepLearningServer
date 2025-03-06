using System.ComponentModel.DataAnnotations;

namespace DeepLearningServer.Models
{
    public class Role
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!; // "PROC엔지니어", "HW엔지니어", "매니저" 등

        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
