namespace Sv.Order.Entities;

public sealed class ProductEntity
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Unit { get; set; }
    public decimal Price { get; set; }
    public decimal StockQuantity { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
