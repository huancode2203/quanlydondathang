using System.ComponentModel.DataAnnotations;

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

public sealed class SaveCustomerRequest
{
    [Required, StringLength(20)] public string Code { get; init; } = string.Empty;
    [Required, StringLength(200)] public string Name { get; init; } = string.Empty;
    [StringLength(20)] public string? Phone { get; init; }
    [EmailAddress, StringLength(255)] public string? Email { get; init; }
    [StringLength(500)] public string? Address { get; init; }
    [StringLength(50)] public string? TaxCode { get; init; }
    [StringLength(1000)] public string? Note { get; init; }
}

public sealed class ProductDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal StockQuantity { get; init; }
    public string? Description { get; init; }
}

public sealed class SaveProductRequest
{
    [Required, StringLength(30)] public string Code { get; init; } = string.Empty;
    [Required, StringLength(255)] public string Name { get; init; } = string.Empty;
    [Required, StringLength(50)] public string Unit { get; init; } = string.Empty;
    [Range(typeof(decimal), "0", "9999999999999999")] public decimal Price { get; init; }
    [Range(typeof(decimal), "0", "9999999999999999")] public decimal StockQuantity { get; init; }
    [StringLength(1000)] public string? Description { get; init; }
}
