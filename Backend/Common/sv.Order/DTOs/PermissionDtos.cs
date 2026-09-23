using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sv.Order.DTOs;

public sealed class PermissionDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Feature { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public sealed class RolePermissionDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsSystemAdmin { get; set; }
    public IReadOnlyList<int> PermissionIds { get; set; } = [];
}

public sealed record PermissionManagementDto(
    IReadOnlyList<RolePermissionDto> Roles,
    IReadOnlyList<PermissionDto> Permissions,
    IReadOnlyList<AccountPermissionDto> Accounts);

public sealed class AccountPermissionDto
{
    public int Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public IReadOnlyList<int> RoleIds { get; set; } = [];
    public IReadOnlyList<string> RoleNames { get; set; } = [];
    public bool IsSystemAdmin { get; set; }
    public bool UsesCustomPermissions { get; init; }
    public IReadOnlyList<int> PermissionIds { get; set; } = [];
}

public sealed class UpdateRolePermissionsRequest : IValidatableObject
{
    [Range(1, int.MaxValue)] public int Id { get; init; }
    [JsonRequired, Required, MaxLength(1000)] public List<int> PermissionIds { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        PermissionRequestValidation.ValidateIds(PermissionIds, nameof(PermissionIds));
}

public sealed class UpdateAccountPermissionsRequest : IValidatableObject
{
    [Range(1, int.MaxValue)] public int Id { get; init; }
    [Required, MinLength(1), MaxLength(100)] public List<int> RoleIds { get; init; } = [];
    [JsonRequired] public bool UsesCustomPermissions { get; init; }
    [JsonRequired, Required, MaxLength(1000)] public List<int> PermissionIds { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        PermissionRequestValidation.ValidateIds(RoleIds, nameof(RoleIds))
            .Concat(PermissionRequestValidation.ValidateIds(PermissionIds, nameof(PermissionIds)));
}

internal static class PermissionRequestValidation
{
    public static IEnumerable<ValidationResult> ValidateIds(List<int>? ids, string memberName)
    {
        if (ids is not null && (ids.Any(id => id <= 0) || ids.Distinct().Count() != ids.Count))
            yield return new("Danh sách mã định danh phải gồm các số nguyên dương và không trùng lặp.", [memberName]);
    }
}
