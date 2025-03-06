using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using DeepLearningServer.Models;
using DeepLearningServer.Enums;

namespace DeepLearningServer.Attributes
{
    public class AuthorizeByPermissionAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        private readonly PermissionType _requiredPermission;

        public AuthorizeByPermissionAttribute(PermissionType requiredPermission)
        {
            _requiredPermission = requiredPermission;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var dbContext = context.HttpContext.RequestServices.GetService(typeof(DlServerContext)) as DlServerContext;
            var userIdClaim = context.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            Console.WriteLine("user id claim: " + userIdClaim);
            if (dbContext == null || string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                context.Result = new UnauthorizedResult(); // 🚀 로그인되지 않은 사용자
                return;
            }
            Console.WriteLine("User id: " + userId);
            // ✅ 유저의 역할(Role) 가져오기
            var userRoles = dbContext.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role.Name)
                .ToHashSet(); // 🚀 HashSet으로 변환하여 검색 속도 최적화

            // ✅ 슈퍼 유저 체크: "SuperAdmin" Role이 있으면 모든 권한 허용
            foreach(var userRole in userRoles)
            {
                Console.WriteLine($"user role: {userRole}");
            }
            if (userRoles.Contains("SuperAdmin"))
            {
                return; // 🚀 모든 권한을 가지고 있으므로 검사 없이 통과
            }

            // ✅ 유저의 역할(Role)에 부여된 권한(Permission) 가져오기
            var userPermissions = dbContext.RolePermissions
                .Where(rp => userRoles.Contains(rp.Role.Name))
                .Select(rp => rp.Permission.Name)
                .ToHashSet(); // 🚀 HashSet으로 변환하여 검색 속도 최적화

            // ✅ 해당 Permission이 없으면 Forbidden 처리
            if (!userPermissions.Contains(_requiredPermission.ToString()))
            {
                context.Result = new ForbidResult(); // 🚀 403 Forbidden
            }
        }
    }
}
