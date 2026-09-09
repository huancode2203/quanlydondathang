namespace Sv.Order.Entities;

public sealed class AccountPermissionEntity
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int PermissionId { get; set; }
    public DateTime GrantedAt { get; set; }
    public AccountEntity Account { get; set; } = null!;
    public PermissionEntity Permission { get; set; } = null!;
}
