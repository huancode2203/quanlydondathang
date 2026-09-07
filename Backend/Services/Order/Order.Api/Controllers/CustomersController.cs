using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sv.Order.DTOs;
using Sv.Order.Repository.Interface;

namespace Order.Api.Controllers;

[ApiController, Authorize]
[Route("api/customers")]
public sealed class CustomersController(IMasterDataRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> GetAll([FromQuery] string? keyword, CancellationToken token)
    {
        if (!HasPermission("CUSTOMER_VIEW")) return PermissionDenied();
        return Ok(await repository.GetCustomersAsync(keyword, token));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(SaveCustomerRequest request, CancellationToken token)
    {
        if (!HasPermission("CUSTOMER_MANAGE")) return PermissionDenied();
        try { var value = await repository.CreateCustomerAsync(request, token); return Created($"api/customers/{value.Id}", value); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomerDto>> Update(int id, SaveCustomerRequest request, CancellationToken token)
    {
        if (!HasPermission("CUSTOMER_MANAGE")) return PermissionDenied();
        try { var value = await repository.UpdateCustomerAsync(id, request, token); return value is null ? NotFound() : Ok(value); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken token)
    {
        if (!HasPermission("CUSTOMER_MANAGE")) return PermissionDenied();
        return await repository.DeleteCustomerAsync(id, token) ? NoContent() : NotFound();
    }

    private bool HasPermission(string code) => User.HasClaim("permission", code);
    private ObjectResult PermissionDenied() => StatusCode(403, new { message = "Tài khoản không có quyền thực hiện chức năng này." });
}
