namespace Sv.Order.Entities;

public sealed class RolePermissionEntity
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    public DateTime GrantedAt { get; set; }
    public RoleEntity Role { get; set; } = null!;
    public PermissionEntity Permission { get; set; } = null!;
}
