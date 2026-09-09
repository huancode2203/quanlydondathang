using System.ComponentModel.DataAnnotations;

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
    public bool IsSystemAdmin { get; init; }
    public IReadOnlyList<int> PermissionIds { get; set; } = [];
}

public sealed record PermissionManagementDto(
    IReadOnlyList<RolePermissionDto> Roles,
    IReadOnlyList<PermissionDto> Permissions);

public sealed class UpdateRolePermissionsRequest
{
    [Required] public List<int> PermissionIds { get; init; } = [];
}
