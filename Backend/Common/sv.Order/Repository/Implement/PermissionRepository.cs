using Dapper;
using Microsoft.EntityFrameworkCore;
using Sv.Order.Data;
using Sv.Order.DTOs;
using Sv.Order.Entities;
using Sv.Order.Repository.Interface;

namespace Sv.Order.Repository.Implement;

public sealed class PermissionRepository(OrderDbContext dbContext) : IPermissionRepository
{
    private const string AdminRoleCode = "ADMIN";

    public async Task<PermissionManagementDto> GetAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT NhomQuyenID AS Id, MaNhomQuyen AS Code, TenNhomQuyen AS Name,
                   MoTa AS Description,
                   CASE WHEN MaNhomQuyen = 'ADMIN' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsSystemAdmin
            FROM tbl_NhomQuyen
            WHERE TrangThai = 'ACTIVE'
            ORDER BY CASE WHEN MaNhomQuyen = 'ADMIN' THEN 0 ELSE 1 END, TenNhomQuyen;

            SELECT QuyenID AS Id, MaQuyen AS Code, TenQuyen AS Name,
                   ISNULL(ChucNang, 'OTHER') AS Feature, ISNULL(HanhDong, '') AS Action,
                   MoTa AS Description
            FROM tbl_Quyen
            WHERE TrangThai = 'ACTIVE'
            ORDER BY ChucNang, QuyenID;

            SELECT NhomQuyenID AS RoleId, QuyenID AS PermissionId
            FROM tbl_CapQuyen;
            """;

        var connection = dbContext.Database.GetDbConnection();
        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
        var roles = (await multi.ReadAsync<RolePermissionDto>()).AsList();
        var permissions = (await multi.ReadAsync<PermissionDto>()).AsList();
        var grants = (await multi.ReadAsync<RolePermissionGrant>()).AsList();
        var activePermissionIds = permissions.Select(x => x.Id).ToHashSet();

        foreach (var role in roles)
        {
            role.PermissionIds = role.IsSystemAdmin
                ? activePermissionIds.Order().ToArray()
                : grants.Where(x => x.RoleId == role.Id && activePermissionIds.Contains(x.PermissionId))
                    .Select(x => x.PermissionId).Distinct().Order().ToArray();
        }

        return new PermissionManagementDto(roles, permissions);
    }

    public async Task<RolePermissionDto?> UpdateRoleAsync(
        int roleId,
        IReadOnlyCollection<int> permissionIds,
        CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles.SingleOrDefaultAsync(
            x => x.Id == roleId && x.Status == "ACTIVE", cancellationToken);
        if (role is null) return null;

        var activePermissionIds = await dbContext.Permissions
            .Where(x => x.Status == "ACTIVE")
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var activePermissionIdSet = activePermissionIds.ToHashSet();
        var isSystemAdmin = role.Code.Equals(AdminRoleCode, StringComparison.OrdinalIgnoreCase);
        var selectedIds = isSystemAdmin
            ? activePermissionIds.Order().ToArray()
            : permissionIds.Distinct().Order().ToArray();

        if (selectedIds.Any(x => !activePermissionIdSet.Contains(x)))
            throw new ArgumentException("Danh sách quyền có quyền không tồn tại hoặc đã ngừng hoạt động.");

        var currentGrants = await dbContext.RolePermissions
            .Where(x => x.RoleId == roleId)
            .ToListAsync(cancellationToken);
        var selectedIdSet = selectedIds.ToHashSet();
        var currentPermissionIdSet = currentGrants.Select(x => x.PermissionId).ToHashSet();
        dbContext.RolePermissions.RemoveRange(currentGrants.Where(x => !selectedIdSet.Contains(x.PermissionId)));
        dbContext.RolePermissions.AddRange(selectedIds
            .Where(permissionId => !currentPermissionIdSet.Contains(permissionId))
            .Select(permissionId => new RolePermissionEntity
        {
            RoleId = roleId,
            PermissionId = permissionId
        }));
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RolePermissionDto
        {
            Id = role.Id,
            Code = role.Code,
            Name = role.Name,
            Description = role.Description,
            IsSystemAdmin = isSystemAdmin,
            PermissionIds = selectedIds
        };
    }

    private sealed class RolePermissionGrant
    {
        public int RoleId { get; init; }
        public int PermissionId { get; init; }
    }
}
