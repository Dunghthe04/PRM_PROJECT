using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Api.Common;

/// <summary>Helper tạo JWT access token cho phiên đăng nhập.</summary>
public static class JwtHelper
{
    /// <summary>
    /// Sinh JWT chứa userId, phone (Name), role — dùng cho [Authorize] / RBAC.
    /// </summary>
    /// <param name="userId">Id user → ClaimTypes.NameIdentifier</param>
    /// <param name="username">Thường là Phone → ClaimTypes.Name</param>
    /// <param name="role">Tên role (Admin, Student, ...) → ClaimTypes.Role</param>
    /// <param name="jwtSettings">Key / Issuer / Audience / Expiry từ appsettings</param>
    public static string GenerateToken(int userId, string username, string role, JwtSettings jwtSettings)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(jwtSettings.Key);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
            Expires = DateTime.UtcNow.AddMinutes(jwtSettings.ExpiryMinutes),
            Issuer = jwtSettings.Issuer,
            Audience = jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
