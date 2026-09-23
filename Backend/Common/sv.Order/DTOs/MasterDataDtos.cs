using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sv.Order.DTOs;

public sealed class CustomerDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? TaxCode { get; init; }
    public string? Note { get; init; }
}

public class SaveCustomerRequest
{
    [Required, StringLength(20)] public string Code { get; init; } = string.Empty;
    [Required, StringLength(200)] public string Name { get; init; } = string.Empty;
    [StringLength(20)] public string? Phone { get; init; }
    [EmailAddress, StringLength(255)] public string? Email { get; init; }
    [StringLength(500)] public string? Address { get; init; }
    [StringLength(50)] public string? TaxCode { get; init; }
    [StringLength(1000)] public string? Note { get; init; }
}

public sealed class UpdateCustomerRequest : SaveCustomerRequest
{
    [Range(1, int.MaxValue)] public int Id { get; init; }
}

public sealed class ProductDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal OrderedQuantity { get; init; }
    public decimal AvailableQuantity { get; init; }
    public decimal StockQuantity { get; init; }
    public string? Description { get; init; }
}

public class SaveProductRequest
{
    [Required, StringLength(30)] public string Code { get; init; } = string.Empty;
    [Required, StringLength(255)] public string Name { get; init; } = string.Empty;
    [Required, StringLength(50)] public string Unit { get; init; } = string.Empty;
    [JsonRequired, DecimalScale(2), Range(typeof(decimal), "0", "999999999999.99")]
    public decimal Price { get; init; }
    [JsonRequired, DecimalScale(2), Range(typeof(decimal), "0", "999999999.99")]
    public decimal StockQuantity { get; init; }
    [StringLength(1000)] public string? Description { get; init; }
}

public sealed class UpdateProductRequest : SaveProductRequest
{
    [Range(1, int.MaxValue)] public int Id { get; init; }
}
