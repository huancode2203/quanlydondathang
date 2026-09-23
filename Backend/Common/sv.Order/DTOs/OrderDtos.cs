using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sv.Order.DTOs;

public sealed class OrderSearchRequest : PagedFilterRequest, IValidatableObject
{
    [RegularExpression("^(CHO_XAC_NHAN|DA_XAC_NHAN|DANG_CHUAN_BI|CHO_GIAO_HANG|DANG_GIAO|DA_GIAO|DA_HUY)$",
        ErrorMessage = "Trạng thái đơn hàng không hợp lệ.")]
    public string? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    [Range(1, int.MaxValue)] public int? CustomerId { get; init; }
    [Range(1, int.MaxValue)] public int? CreatorEmployeeId { get; init; }
    [Range(1, int.MaxValue)] public int? DeliveryEmployeeId { get; init; }
    [DecimalScale(2), Range(typeof(decimal), "0", "999999999999.99")]
    public decimal? MinTotal { get; init; }
    [DecimalScale(2), Range(typeof(decimal), "0", "999999999999.99")]
    public decimal? MaxTotal { get; init; }
    [RegularExpression("^(orderedDateDesc|deliveryDateAsc|deliveryDateDesc|totalAsc|totalDesc)$",
        ErrorMessage = "Kiểu sắp xếp không hợp lệ.")]
    public string? Sort { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FromDate?.Date > ToDate?.Date)
            yield return new("Từ ngày không được lớn hơn đến ngày.", [nameof(FromDate), nameof(ToDate)]);
        if (ToDate?.Date == DateTime.MaxValue.Date)
            yield return new("Đến ngày phải nhỏ hơn 31/12/9999.", [nameof(ToDate)]);
        if (MinTotal > MaxTotal)
            yield return new("Tổng tiền nhỏ nhất không được lớn hơn tổng tiền lớn nhất.", [nameof(MinTotal), nameof(MaxTotal)]);
    }
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

public class SaveOrderRequest : IValidatableObject
{
    [Required, StringLength(30)] public string Code { get; init; } = string.Empty;
    [Range(1, int.MaxValue)] public int CustomerId { get; init; }
    [Range(1, int.MaxValue)] public int? DeliveryEmployeeId { get; init; }
    [JsonRequired] public DateTime OrderedAt { get; init; }
    [Required] public DateTime? ExpectedDeliveryAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    [Required, StringLength(500)] public string DeliveryAddress { get; init; } = string.Empty;
    [DecimalScale(2), Range(typeof(decimal), "0", "999999999999.99")]
    public decimal DiscountAmount { get; init; }
    [DecimalScale(2), Range(typeof(decimal), "0", "999999999999.99")]
    public decimal TaxAmount { get; init; }
    [DecimalScale(2), Range(typeof(decimal), "0", "999999999999.99")]
    public decimal ShippingFee { get; init; }
    [Required, RegularExpression("^(CHO_XAC_NHAN|DA_XAC_NHAN|DANG_CHUAN_BI|CHO_GIAO_HANG|DANG_GIAO|DA_GIAO|DA_HUY)$",
        ErrorMessage = "Trạng thái đơn hàng không hợp lệ.")]
    public string Status { get; init; } = "CHO_XAC_NHAN";
    [StringLength(1000)] public string? Note { get; init; }
    [Required, MinLength(1), MaxLength(200)] public List<SaveOrderItemRequest> Items { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Items is not null && Items.Any(item => item is null))
            yield return new("Chi tiết hàng hóa không được chứa dòng trống.", [nameof(Items)]);
    }
}

public sealed class UpdateOrderRequest : SaveOrderRequest
{
    [Range(1L, long.MaxValue)] public long Id { get; init; }
}

public sealed class SaveOrderItemRequest
{
    [Range(1, int.MaxValue)] public int ProductId { get; init; }
    [DecimalScale(2), Range(typeof(decimal), "0.01", "999999999.99")] public decimal Quantity { get; init; }
    [JsonRequired, DecimalScale(2), Range(typeof(decimal), "0", "999999999999.99")] public decimal UnitPrice { get; init; }
    [DecimalScale(2), Range(typeof(decimal), "0", "100")] public decimal DiscountPercent { get; init; }
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
