using Microsoft.AspNetCore.Mvc;
using Order.Api.Infrastructure;
using Sv.Order.DTOs;
using Sv.Order.Repository.Interface;

namespace Order.Api.Controllers;

[Route("api/customers")]
public sealed class CustomersController(IMasterDataRepository repository) : ApiControllerBase
{
    [HttpPost("search"), RequirePermission("CUSTOMER_VIEW")]
    public async Task<IActionResult> Search(SearchRequest request, CancellationToken token) =>
        Success(await repository.GetCustomersAsync(request.Keyword, token));

    [HttpPost("create"), RequirePermission("CUSTOMER_MANAGE")]
    public async Task<IActionResult> Create(SaveCustomerRequest request, CancellationToken token) =>
        Success(await repository.CreateCustomerAsync(request, token), "Đã tạo khách hàng.", 201);

    [HttpPost("update"), RequirePermission("CUSTOMER_MANAGE")]
    public async Task<IActionResult> Update(UpdateCustomerRequest request, CancellationToken token)
    {
        var value = await repository.UpdateCustomerAsync(request.Id, request, token);
        return value is null ? Missing("Không tìm thấy khách hàng.") : Success(value, "Đã cập nhật khách hàng.");
    }

    [HttpPost("delete"), RequirePermission("CUSTOMER_MANAGE")]
    public async Task<IActionResult> Delete(IdRequest request, CancellationToken token) =>
        await repository.DeleteCustomerAsync(request.Id, token)
            ? Success<object?>(null, "Đã xóa khách hàng.")
            : Missing("Không tìm thấy khách hàng.");
}
