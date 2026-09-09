namespace Sv.Order.Entities;

public sealed class PermissionEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Feature { get; set; }
    public string? Action { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public DateTime CreatedAt { get; set; }
    public List<RolePermissionEntity> Roles { get; set; } = [];
}
