namespace Sv.Order.Entities;

public sealed class AccountRoleEntity
{
    public long Id { get; set; }
    public int AccountId { get; set; }
    public int RoleId { get; set; }
    public DateTime AssignedAt { get; set; }
}
