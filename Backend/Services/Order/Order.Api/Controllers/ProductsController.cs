using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sv.Order.DTOs;
using Sv.Order.Repository.Interface;

namespace Order.Api.Controllers;

[ApiController, Authorize]
[Route("api/products")]
public sealed class ProductsController(IMasterDataRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll([FromQuery] string? keyword, CancellationToken token)
    {
        if (!HasPermission("PRODUCT_VIEW")) return PermissionDenied();
        return Ok(await repository.GetProductsAsync(keyword, token));
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(SaveProductRequest request, CancellationToken token)
    {
        if (!HasPermission("PRODUCT_MANAGE")) return PermissionDenied();
        try { var value = await repository.CreateProductAsync(request, token); return Created($"api/products/{value.Id}", value); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> Update(int id, SaveProductRequest request, CancellationToken token)
    {
        if (!HasPermission("PRODUCT_MANAGE")) return PermissionDenied();
        try { var value = await repository.UpdateProductAsync(id, request, token); return value is null ? NotFound() : Ok(value); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken token)
    {
        if (!HasPermission("PRODUCT_MANAGE")) return PermissionDenied();
        return await repository.DeleteProductAsync(id, token) ? NoContent() : NotFound();
    }

    private bool HasPermission(string code) => User.HasClaim("permission", code);
    private ObjectResult PermissionDenied() => StatusCode(403, new { message = "Tài khoản không có quyền thực hiện chức năng này." });
}
