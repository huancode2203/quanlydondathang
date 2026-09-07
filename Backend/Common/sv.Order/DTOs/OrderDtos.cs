using System.ComponentModel.DataAnnotations;

namespace Sv.Order.DTOs;

public sealed class OrderSearchRequest
{
    public string? Keyword { get; init; }
    public string? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int? CustomerId { get; init; }
    public int? CreatorEmployeeId { get; init; }
    public int? DeliveryEmployeeId { get; init; }
    public decimal? MinTotal { get; init; }
    public decimal? MaxTotal { get; init; }
    public string? Sort { get; init; }
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 10;
}

public class OrderListItemDto
{
    public long Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public string CreatorName { get; init; } = string.Empty;
    public string? DeliveryEmployeeName { get; init; }
    public DateTime OrderedAt { get; init; }
    public DateTime? ExpectedDeliveryAt { get; init; }
    public string DeliveryAddress { get; init; } = string.Empty;
    public decimal GrandTotal { get; init; }
    public string Status { get; init; } = string.Empty;
    public int ItemCount { get; init; }
}

public sealed class OrderDetailDto : OrderListItemDto
{
    public int CreatorEmployeeId { get; init; }
    public int? DeliveryEmployeeId { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public decimal MerchandiseTotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal ShippingFee { get; init; }
    public string? Note { get; init; }
    public IReadOnlyList<OrderItemDto> Items { get; set; } = [];
}

public sealed class OrderItemDto
{
    public long Id { get; init; }
    public int ProductId { get; init; }
    public string ProductCode { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal DiscountPercent { get; init; }
    public decimal LineTotal { get; init; }
    public decimal StockQuantity { get; init; }
    public string? Note { get; init; }
}

public sealed class SaveOrderRequest
{
    [Required, StringLength(30)] public string Code { get; init; } = string.Empty;
    [Range(1, int.MaxValue)] public int CustomerId { get; init; }
    public int? DeliveryEmployeeId { get; init; }
    public DateTime OrderedAt { get; init; }
    [Required] public DateTime? ExpectedDeliveryAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    [Required, StringLength(500)] public string DeliveryAddress { get; init; } = string.Empty;
    [Range(0, double.MaxValue)] public decimal DiscountAmount { get; init; }
    [Range(0, double.MaxValue)] public decimal TaxAmount { get; init; }
    [Range(0, double.MaxValue)] public decimal ShippingFee { get; init; }
    [Required, StringLength(30)] public string Status { get; init; } = "CHO_XAC_NHAN";
    [StringLength(1000)] public string? Note { get; init; }
    [Required, MinLength(1)] public List<SaveOrderItemRequest> Items { get; init; } = [];
}

public sealed class SaveOrderItemRequest
{
    [Range(1, int.MaxValue)] public int ProductId { get; init; }
    [Range(typeof(decimal), "0.01", "9999999999999999")] public decimal Quantity { get; init; }
    [Range(typeof(decimal), "0", "9999999999999999")] public decimal UnitPrice { get; init; }
    [Range(typeof(decimal), "0", "100")] public decimal DiscountPercent { get; init; }
    [StringLength(500)] public string? Note { get; init; }
}

public sealed class LookupDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Extra { get; init; }
    public decimal? Price { get; init; }
    public decimal? StockQuantity { get; init; }
}
public sealed record OrderLookupsDto(IReadOnlyList<LookupDto> Customers, IReadOnlyList<LookupDto> Products, IReadOnlyList<LookupDto> Employees);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
}
