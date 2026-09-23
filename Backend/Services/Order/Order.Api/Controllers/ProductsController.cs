using Microsoft.AspNetCore.Mvc;
using Order.Api.Infrastructure;
using Sv.Order.DTOs;
using Sv.Order.Repository.Interface;

namespace Order.Api.Controllers;

[Route("api/products")]
public sealed class ProductsController(IMasterDataRepository repository) : ApiControllerBase
{
    [HttpPost("search"), RequirePermission("PRODUCT_VIEW")]
    public async Task<IActionResult> Search(SearchRequest request, CancellationToken token) =>
        Success(await repository.GetProductsAsync(request.Keyword, token));

    [HttpPost("create"), RequirePermission("PRODUCT_MANAGE")]
    public async Task<IActionResult> Create(SaveProductRequest request, CancellationToken token) =>
        Success(await repository.CreateProductAsync(request, token), "Đã tạo hàng hóa.", 201);

    [HttpPost("update"), RequirePermission("PRODUCT_MANAGE")]
    public async Task<IActionResult> Update(UpdateProductRequest request, CancellationToken token)
    {
        var value = await repository.UpdateProductAsync(request.Id, request, token);
        return value is null ? Missing("Không tìm thấy hàng hóa.") : Success(value, "Đã cập nhật hàng hóa.");
    }

    [HttpPost("delete"), RequirePermission("PRODUCT_MANAGE")]
    public async Task<IActionResult> Delete(IdRequest request, CancellationToken token) =>
        await repository.DeleteProductAsync(request.Id, token)
            ? Success<object?>(null, "Đã xóa hàng hóa.")
            : Missing("Không tìm thấy hàng hóa.");
}
