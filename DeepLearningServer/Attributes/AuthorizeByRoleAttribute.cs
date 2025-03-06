using Microsoft.AspNetCore.Authorization;
using DeepLearningServer.Enums;
using System;

namespace DeepLearningServer.Attributes
{
    public class AuthorizeByRoleAttribute : AuthorizeAttribute
    {
        public AuthorizeByRoleAttribute(params UserRoleType[] roles)
        {
            Roles = string.Join(",", roles.Select(r => r.ToString())); // Enum → 문자열 변환
        }
    }
}
