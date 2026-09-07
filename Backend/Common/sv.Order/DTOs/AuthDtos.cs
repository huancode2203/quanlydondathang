using System.ComponentModel.DataAnnotations;

namespace Sv.Order.DTOs;

public sealed class LoginRequest
{
    [Required, StringLength(100)] public string Username { get; init; } = string.Empty;
    [Required, StringLength(100)] public string Password { get; init; } = string.Empty;
}

public sealed class AuthUserDto
{
    public int AccountId { get; init; }
    public int EmployeeId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
    public IReadOnlyList<string> Permissions { get; set; } = [];
}

public sealed record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    int EmployeeId,
    string Username,
    string FullName,
    string RoleName,
    IReadOnlyList<string> Permissions);
