using Sv.Order.DTOs;

namespace Sv.Order.Repository.Interface;

public interface IMasterDataRepository
{
    Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(string? keyword, CancellationToken cancellationToken);
    Task<CustomerDto> CreateCustomerAsync(SaveCustomerRequest request, CancellationToken cancellationToken);
    Task<CustomerDto?> UpdateCustomerAsync(int id, SaveCustomerRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteCustomerAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductDto>> GetProductsAsync(string? keyword, CancellationToken cancellationToken);
    Task<ProductDto> CreateProductAsync(SaveProductRequest request, CancellationToken cancellationToken);
    Task<ProductDto?> UpdateProductAsync(int id, SaveProductRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteProductAsync(int id, CancellationToken cancellationToken);
}
