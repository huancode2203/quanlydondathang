namespace Sv.Order.Entities;

public sealed class OrderItemEntity
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal LineTotal { get; private set; }
    public string? Note { get; set; }
    public OrderEntity Order { get; set; } = null!;
}
