namespace Sv.Order.Entities;

public sealed class OrderEntity
{
    public long Id { get; set; }
    public required string Code { get; set; }
    public int CustomerId { get; set; }
    public int ManagerEmployeeId { get; set; }
    public int? DeliveryEmployeeId { get; set; }
    public DateTime OrderedAt { get; set; }
    public DateTime? ExpectedDeliveryAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public required string DeliveryAddress { get; set; }
    public decimal MerchandiseTotal { get; private set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal GrandTotal { get; private set; }
    public required string Status { get; set; }
    public bool StockDeducted { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; set; }
    public List<OrderItemEntity> Items { get; set; } = [];
}
