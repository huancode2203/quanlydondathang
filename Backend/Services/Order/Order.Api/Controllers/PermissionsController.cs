using Microsoft.AspNetCore.Mvc;
using Order.Api.Infrastructure;
using Sv.Order.DTOs;
using Sv.Order.Repository.Interface;

namespace Order.Api.Controllers;

[Route("api/permissions"), RequirePermission("PERMISSION_MANAGE")]
public sealed class PermissionsController(IPermissionRepository repository) : ApiControllerBase
{
    [HttpPost("search")]
    public async Task<IActionResult> Get(EmptyRequest request, CancellationToken token) =>
        Success(await repository.GetAsync(token));

    [HttpPost("roles/update")]
    public async Task<IActionResult> UpdateRole(UpdateRolePermissionsRequest request, CancellationToken token)
    {
        var role = await repository.UpdateRoleAsync(request.Id, request.PermissionIds, token);
        return role is null ? Missing("Không tìm thấy nhóm quyền.") : Success(role, "Đã cập nhật nhóm quyền.");
    }

    [HttpPost("accounts/update")]
    public async Task<IActionResult> UpdateAccount(UpdateAccountPermissionsRequest request, CancellationToken token)
    {
        var account = await repository.UpdateAccountAsync(request.Id, request.RoleIds,
            request.UsesCustomPermissions, request.PermissionIds, token);
        return account is null ? Missing("Không tìm thấy tài khoản.") : Success(account, "Đã cập nhật quyền tài khoản.");
    }
}
