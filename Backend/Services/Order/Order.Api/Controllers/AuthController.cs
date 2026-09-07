using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Sv.Order.DTOs;
using Sv.Order.Repository.Interface;

namespace Order.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthRepository repository, IConfiguration configuration) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await repository.AuthenticateAsync(request.Username, request.Password, cancellationToken);
        if (user is null) return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không đúng." });

        var expiresAt = DateTime.UtcNow.AddHours(8);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.AccountId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("employee_id", user.EmployeeId.ToString()),
            new("full_name", user.FullName),
            new(ClaimTypes.Role, user.RoleName)
        };
        claims.AddRange(user.Permissions.Select(x => new Claim("permission", x)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        var tokenText = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(new LoginResponse(tokenText, expiresAt, user.EmployeeId, user.Username,
            user.FullName, user.RoleName, user.Permissions));
    }
}
