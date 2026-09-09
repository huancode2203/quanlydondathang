using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sv.Order.DTOs;
using Sv.Order.Repository.Interface;

namespace Order.Api.Controllers;

[ApiController, Authorize]
[Route("api/permissions")]
public sealed class PermissionsController(IPermissionRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PermissionManagementDto>> Get(CancellationToken cancellationToken)
    {
        if (!CanManagePermissions()) return PermissionDenied();
        return Ok(await repository.GetAsync(cancellationToken));
    }

    [HttpPut("roles/{roleId:int}")]
    public async Task<ActionResult<RolePermissionDto>> UpdateRole(
        int roleId,
        UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        if (!CanManagePermissions()) return PermissionDenied();

        try
        {
            var role = await repository.UpdateRoleAsync(roleId, request.PermissionIds, cancellationToken);
            return role is null ? NotFound(new { message = "Không tìm thấy nhóm quyền." }) : Ok(role);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Không thể cập nhật quyền do dữ liệu vừa thay đổi. Vui lòng tải lại trang." });
        }
    }

    private bool CanManagePermissions() => User.HasClaim("permission", "PERMISSION_MANAGE");
    private ObjectResult PermissionDenied() =>
        StatusCode(403, new { message = "Tài khoản không có quyền phân quyền." });
}
