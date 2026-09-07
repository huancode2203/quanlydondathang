using Sv.Order.DTOs;

namespace Sv.Order.Repository.Interface;

public interface IOrderRepository
{
    Task<PagedResult<OrderListItemDto>> SearchAsync(OrderSearchRequest request, CancellationToken cancellationToken);
    Task<OrderDetailDto?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<OrderLookupsDto> GetLookupsAsync(CancellationToken cancellationToken);
    Task<OrderDetailDto> CreateAsync(SaveOrderRequest request, int creatorEmployeeId, CancellationToken cancellationToken);
    Task<OrderDetailDto?> UpdateAsync(long id, SaveOrderRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken);
}
