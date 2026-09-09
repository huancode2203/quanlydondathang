using Sv.Order.DTOs;

namespace Sv.Order.Repository.Interface;

public interface IPermissionRepository
{
    Task<PermissionManagementDto> GetAsync(CancellationToken cancellationToken);
    Task<RolePermissionDto?> UpdateRoleAsync(int roleId, IReadOnlyCollection<int> permissionIds, CancellationToken cancellationToken);
}
