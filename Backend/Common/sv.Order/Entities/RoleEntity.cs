namespace Sv.Order.Entities;

public sealed class RoleEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public DateTime CreatedAt { get; set; }
    public List<RolePermissionEntity> Permissions { get; set; } = [];
}
