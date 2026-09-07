namespace Sv.Order.Entities;

public sealed class CustomerEntity
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
