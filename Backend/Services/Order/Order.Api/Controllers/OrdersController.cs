using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sv.Order.DTOs;
using Sv.Order.Repository.Interface;

namespace Order.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public sealed class OrdersController(IOrderRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderListItemDto>>> Search([FromQuery] OrderSearchRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission("ORDER_VIEW")) return PermissionDenied();
        try { return Ok(await repository.SearchAsync(request, cancellationToken)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("lookups")]
    public async Task<ActionResult<OrderLookupsDto>> GetLookups(CancellationToken cancellationToken)
    {
        if (!HasPermission("ORDER_VIEW")) return PermissionDenied();
        return Ok(await repository.GetLookupsAsync(cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<OrderDetailDto>> GetById(long id, CancellationToken cancellationToken)
    {
        if (!HasPermission("ORDER_VIEW")) return PermissionDenied();
        var order = await repository.GetByIdAsync(id, cancellationToken);
        return order is null ? NotFound(new { message = "Không tìm thấy đơn hàng." }) : Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<OrderDetailDto>> Create([FromBody] SaveOrderRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission("ORDER_CREATE")) return PermissionDenied();
        try
        {
            var order = await repository.CreateAsync(request, GetEmployeeId(), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (DbUpdateException) { return BadRequest(new { message = "Dữ liệu khách hàng, nhân viên hoặc hàng hóa không hợp lệ." }); }
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<OrderDetailDto>> Update(long id, [FromBody] SaveOrderRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission("ORDER_UPDATE")) return PermissionDenied();
        try
        {
            var order = await repository.UpdateAsync(id, request, cancellationToken);
            return order is null ? NotFound(new { message = "Không tìm thấy đơn hàng." }) : Ok(order);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (DbUpdateException) { return BadRequest(new { message = "Dữ liệu khách hàng, nhân viên hoặc hàng hóa không hợp lệ." }); }
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        if (!HasPermission("ORDER_DELETE")) return PermissionDenied();
        try
        {
            return await repository.DeleteAsync(id, cancellationToken)
                ? NoContent()
                : NotFound(new { message = "Không tìm thấy đơn hàng." });
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    private bool HasPermission(string permissionCode) => User.HasClaim("permission", permissionCode);
    private int GetEmployeeId() => int.Parse(User.FindFirstValue("employee_id")
        ?? throw new InvalidOperationException("Token không chứa mã nhân viên."));

    private ObjectResult PermissionDenied() => StatusCode(StatusCodes.Status403Forbidden,
        new { message = "Tài khoản không có quyền thực hiện chức năng này." });
}
