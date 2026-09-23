using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Order.Api.Infrastructure;
using Sv.Order.DTOs;
using Sv.Order.Repository.Interface;

namespace Order.Api.Controllers;

[Route("api/orders")]
public sealed class OrdersController(IOrderRepository repository) : ApiControllerBase
{
    [HttpPost("search"), RequirePermission("ORDER_VIEW")]
    public async Task<IActionResult> Search(OrderSearchRequest request, CancellationToken token) =>
        Success(await repository.SearchAsync(request, token));

    [HttpPost("lookups"), RequirePermission("ORDER_VIEW")]
    public async Task<IActionResult> GetLookups(EmptyRequest request, CancellationToken token) =>
        Success(await repository.GetLookupsAsync(token));

    [HttpPost("detail"), RequirePermission("ORDER_VIEW")]
    public async Task<IActionResult> GetById(OrderIdRequest request, CancellationToken token)
    {
        var order = await repository.GetByIdAsync(request.Id, token);
        return order is null ? Missing("Không tìm thấy đơn hàng.") : Success(order);
    }

    [HttpPost("create"), RequirePermission("ORDER_CREATE")]
    public async Task<IActionResult> Create(SaveOrderRequest request, CancellationToken token)
    {
        // The authentication handler has already validated this trusted claim.
        var employeeId = int.Parse(User.FindFirstValue("employee_id")!);
        return Success(await repository.CreateAsync(request, employeeId, token), "Đã tạo đơn hàng.", 201);
    }

    [HttpPost("update"), RequirePermission("ORDER_VIEW")]
    public async Task<IActionResult> Update(UpdateOrderRequest request, CancellationToken token)
    {
        var current = await repository.GetByIdAsync(request.Id, token);
        if (current is null) return Missing("Không tìm thấy đơn hàng.");

        var requiredPermission = RequiredUpdatePermission(current.Status, request.Status);
        if (!User.HasClaim("permission", requiredPermission))
            return Forbidden(requiredPermission switch
            {
                "ORDER_APPROVE" => "Cần quyền duyệt đơn để chuyển trạng thái này.",
                "ORDER_DELIVERY" => "Cần quyền giao hàng để chuyển trạng thái này.",
                _ => "Tài khoản không có quyền cập nhật đơn hàng."
            });
        if (!User.HasClaim("permission", "ORDER_UPDATE") && HasOrderContentChanged(current, request))
            return Forbidden("Tài khoản chỉ có quyền đổi trạng thái đơn hàng, không được sửa thông tin hoặc mặt hàng.");

        var order = await repository.UpdateAsync(request.Id, request, token);
        return order is null ? Missing("Không tìm thấy đơn hàng.") : Success(order, "Đã cập nhật đơn hàng.");
    }

    private static string RequiredUpdatePermission(string currentStatus, string nextStatus)
    {
        if (currentStatus == nextStatus) return "ORDER_UPDATE";
        if (nextStatus == "DA_HUY")
            return currentStatus is "CHO_GIAO_HANG" or "DANG_GIAO" ? "ORDER_DELIVERY" : "ORDER_APPROVE";
        return nextStatus is "DA_XAC_NHAN" or "DANG_CHUAN_BI" ? "ORDER_APPROVE" : "ORDER_DELIVERY";
    }

    private static bool HasOrderContentChanged(OrderDetailDto current, UpdateOrderRequest request) =>
        current.Code != request.Code.Trim() || current.CustomerId != request.CustomerId ||
        current.OrderedAt != request.OrderedAt || current.ExpectedDeliveryAt != request.ExpectedDeliveryAt ||
        current.DeliveryAddress != request.DeliveryAddress.Trim() || current.DiscountAmount != request.DiscountAmount ||
        current.TaxAmount != request.TaxAmount || current.ShippingFee != request.ShippingFee ||
        current.Note != request.Note?.Trim() || current.Items.Count != request.Items.Count ||
        current.Items.Zip(request.Items).Any(pair => pair.First.ProductId != pair.Second.ProductId ||
            pair.First.Quantity != pair.Second.Quantity || pair.First.UnitPrice != pair.Second.UnitPrice ||
            pair.First.DiscountPercent != pair.Second.DiscountPercent || pair.First.Note != pair.Second.Note?.Trim());

    [HttpPost("delete"), RequirePermission("ORDER_DELETE")]
    public async Task<IActionResult> Delete(OrderIdRequest request, CancellationToken token) =>
        await repository.DeleteAsync(request.Id, token)
            ? Success<object?>(null, "Đã xóa đơn hàng.")
            : Missing("Không tìm thấy đơn hàng.");
}
