using DeepLearningServer.Enums;
using DeepLearningServer.Models;
using DeepLearningServer.Settings;

namespace DeepLearningServer.Classes
{
    public class DbInitializer
    {
        public static void Initialize(DlServerContext context, ServerSettings serverSettings)
        {
            if (!context.Roles.Any())
            {
                var roles = Enum.GetValues(typeof(UserRoleType))
                    .Cast<UserRoleType>()
                    .Select(role => new Role { Name = role.ToString() })
                    .ToList();

                context.Roles.AddRange(roles);
                context.SaveChangesAsync().GetAwaiter().GetResult();
            }
            if (!context.Permissions.Any())
            {
                var permissions = Enum.GetValues(typeof(PermissionType))
                    .Cast<PermissionType>()
                    .Select(permission => new Permission { Name = permission.ToString() })
                    .ToList();

                context.Permissions.AddRange(permissions);
                context.SaveChangesAsync().GetAwaiter().GetResult();
            }
            // 🔥 기본 역할에 기본 권한 추가
            var managerRole = context.Roles.FirstOrDefault(r => r.Name == UserRoleType.Manager.ToString());
            var runModelPermission = context.Permissions.FirstOrDefault(p => p.Name == PermissionType.RunModel.ToString());

            if (managerRole != null && runModelPermission != null &&
                !context.RolePermissions.Any(rp => rp.RoleId == managerRole.Id && rp.PermissionId == runModelPermission.Id))
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = managerRole.Id,
                    PermissionId = runModelPermission.Id
                });
                context.SaveChangesAsync().GetAwaiter().GetResult();
            }
            // 4️⃣ 슈퍼 계정 (ADMIN) 생성
            if (serverSettings.EnableAdminSeed)
            {
                // 🔥 ADMIN 유저 확인 (없으면 생성)
                var existingAdmin = context.Users.FirstOrDefault(u => u.Username == "ADMIN");
                if (existingAdmin == null)
                {
                    User adminUser = new()
                    {
                        Email = null,
                        Username = serverSettings.AdminSeedName,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(serverSettings.AdminSeedPassword), // 🚀 ADMIN 기본 비밀번호
                        IsActive = true
                    };

                    context.Users.Add(adminUser);
                    context.SaveChangesAsync().GetAwaiter().GetResult();

                    var adminRole = context.Roles.FirstOrDefault(r => r.Name == UserRoleType.ServiceEngineer.ToString());
                    // 🔥 ADMIN 계정 Role 생성 (슈퍼 유저 역할)
                    if(adminRole == null)
                    {
                        adminRole = new()
                        {
                            Name = UserRoleType.ServiceEngineer.ToString()
                        };
                        context.Roles.Add(adminRole);
                        context.SaveChangesAsync().GetAwaiter().GetResult();
                    }
                   

                    

                    // 🔥 ADMIN 계정에 Role 할당
                    UserRole userRole = new UserRole
                    {
                        UserId = adminUser.Id,
                        RoleId = adminRole.Id
                    };

                    context.UserRoles.Add(userRole);
                    context.SaveChangesAsync().GetAwaiter().GetResult();

                    // 🔥 ADMIN 계정에 모든 권한 부여
                    var allPermissions = context.Permissions.ToList();
                    foreach (var permission in allPermissions)
                    {
                        context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = adminRole.Id,
                            PermissionId = permission.Id
                        });
                    }

                    context.SaveChangesAsync().GetAwaiter().GetResult();
                    Console.WriteLine("🔥 SuperAdmin 계정이 성공적으로 생성되었습니다! 모든 권한이 부여되었습니다.");
                }
            }
        }
    }
}
