using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using DeepLearningServer.Models;
using Microsoft.AspNetCore.Identity.Data;
using DeepLearningServer.Enums;

/// <summary>
/// 사용자 인증과 관련된 기능을 제공하는 컨트롤러입니다.
/// </summary>
namespace DeepLearningServer.Controllers;
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly DlServerContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(DlServerContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    /// <summary>
    /// 새로운 사용자를 등록합니다.
    /// </summary>
    /// <param name="request">
    /// 사용자 등록 정보:
    /// - Username: 사용자 아이디 (중복 불가)
    /// - Password: 사용자 비밀번호
    /// - Email: 사용자 이메일 (중복 불가)
    /// </param>
    /// <returns>사용자 등록 성공 메시지</returns>
    /// <response code="200">등록 성공</response>
    /// <response code="400">사용자명 또는 이메일이 이미 존재함</response>
    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        if (_context.Users.Any(u => u.Username == request.Username || u.Email == request.Email))
            return BadRequest("Username or Email already exists");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        var role = _context.Roles.FirstOrDefault(r => r.Name == UserRoleType.Operator.ToString());
        if (role == null)
            return BadRequest("Invalid role. Check if role exists.");

        var userRole = new UserRole { UserId = user.Id, RoleId = role.Id };
        _context.UserRoles.Add(userRole);
        _context.SaveChanges();

        return Ok(new { message = "User registered successfully" });
    }

    /// <summary>
    /// 사용자 로그인을 처리하고 JWT 토큰을 발급합니다.
    /// </summary>
    /// <param name="loginRequest">
    /// 로그인 정보:
    /// - Username: 사용자 아이디
    /// - Password: 사용자 비밀번호
    /// </param>
    /// <returns>JWT 인증 토큰</returns>
    /// <response code="200">로그인 성공 및 토큰 발급</response>
    /// <response code="401">잘못된 사용자 자격 증명</response>
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest loginRequest)
    {
        var user = _context.Users.FirstOrDefault(u => u.Username == loginRequest.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials");

        var token = GenerateJwtToken(user);
        return Ok(new { Token = token });
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Username),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim("UserId", user.Id.ToString())
    };

        // ✅ 유저가 속한 Role을 가져옴
        var userRoles = _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.Role.Name)
            .ToHashSet(); // 🚀 HashSet으로 변환하여 검색 속도 향상
        foreach (var userRole in userRoles)
        {
            Console.WriteLine($"user role: {userRole}");
        }
        // ✅ 해당 Role에 부여된 Permission을 가져옴
        var userPermissions = _context.RolePermissions
            .Where(rp => userRoles.Contains(rp.Role.Name))
            .Select(rp => rp.Permission.Name)
            .ToList();
        foreach (var userPermission in userPermissions)
        {
            Console.WriteLine($"user permission: {userPermission}");
        }
        // ✅ Role 추가
        claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        // ✅ Permission 추가
        claims.AddRange(userPermissions.Select(permission => new Claim("Permission", permission)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            null,
            null,
            claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}

/// <summary>
/// 로그인 요청 모델입니다.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// 사용자 아이디
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// 사용자 비밀번호
    /// </summary>
    [DataType(DataType.Password)]
    public string Password { get; set; }
}

/// <summary>
/// 사용자 등록 요청 모델입니다.
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// 등록할 사용자 아이디 (고유값)
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// 등록할 사용자 비밀번호 (평문)
    /// </summary>
    [DataType(DataType.Password)]
    public string Password { get; set; }

    /// <summary>
    /// 등록할 사용자 이메일 (고유값)
    /// </summary>
    public string Email { get; set; }
    //public UserRoleType Role { get; set; } // Enum 값으로 역할을 입력받음
}