using System.ComponentModel.DataAnnotations;

namespace DeepLearningServer.Models
{
    public class Permission
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!; // "모델 훈련", "모델 검증", "모델 배포" 등

        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
