namespace Sv.Order.Entities;

public sealed class AccountEntity
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int RoleId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE";
    public bool UsesCustomPermissions { get; set; }
    public RoleEntity Role { get; set; } = null!;
    public List<AccountPermissionEntity> Permissions { get; set; } = [];
}
