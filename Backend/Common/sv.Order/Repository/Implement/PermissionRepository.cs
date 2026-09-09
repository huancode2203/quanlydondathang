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

            SELECT tk.TaiKhoanID AS Id, tk.TenDangNhap AS Username, nv.HoTen AS FullName,
                   tk.NhomQuyenID AS RoleId, nq.MaNhomQuyen AS RoleCode,
                   nq.TenNhomQuyen AS RoleName,
                   CASE WHEN nq.MaNhomQuyen = 'ADMIN' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsSystemAdmin,
                   tk.SuDungQuyenRieng AS UsesCustomPermissions
            FROM tbl_TaiKhoan tk
            INNER JOIN tbl_NhanVien nv ON nv.NhanVienID = tk.NhanVienID
            INNER JOIN tbl_NhomQuyen nq ON nq.NhomQuyenID = tk.NhomQuyenID
            WHERE tk.TrangThai = 'ACTIVE' AND nv.TrangThai = 'ACTIVE'
              AND nv.IsDeleted = 0 AND nq.TrangThai = 'ACTIVE'
            ORDER BY CASE WHEN nq.MaNhomQuyen = 'ADMIN' THEN 0 ELSE 1 END, nv.HoTen;

            SELECT TaiKhoanID AS AccountId, QuyenID AS PermissionId
            FROM tbl_CapQuyenTaiKhoan;
            """;

        var connection = dbContext.Database.GetDbConnection();
        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
        var roles = (await multi.ReadAsync<RolePermissionDto>()).AsList();
        var permissions = (await multi.ReadAsync<PermissionDto>()).AsList();
        var grants = (await multi.ReadAsync<RolePermissionGrant>()).AsList();
        var accounts = (await multi.ReadAsync<AccountPermissionDto>()).AsList();
        var accountGrants = (await multi.ReadAsync<AccountPermissionGrant>()).AsList();
        var activePermissionIds = permissions.Select(x => x.Id).ToHashSet();

        foreach (var role in roles)
        {
            role.PermissionIds = role.IsSystemAdmin
                ? activePermissionIds.Order().ToArray()
                : grants.Where(x => x.RoleId == role.Id && activePermissionIds.Contains(x.PermissionId))
                    .Select(x => x.PermissionId).Distinct().Order().ToArray();
        }

        var roleLookup = roles.ToDictionary(x => x.Id);
        foreach (var account in accounts)
        {
            account.PermissionIds = account.IsSystemAdmin
                ? activePermissionIds.Order().ToArray()
                : account.UsesCustomPermissions
                    ? accountGrants.Where(x => x.AccountId == account.Id && activePermissionIds.Contains(x.PermissionId))
                        .Select(x => x.PermissionId).Distinct().Order().ToArray()
                    : roleLookup.GetValueOrDefault(account.RoleId)?.PermissionIds ?? [];
        }

        return new PermissionManagementDto(roles, permissions, accounts);
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

    public async Task<AccountPermissionDto?> UpdateAccountAsync(
        int accountId,
        int roleId,
        bool usesCustomPermissions,
        IReadOnlyCollection<int> permissionIds,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.Accounts.Include(x => x.Role).SingleOrDefaultAsync(
            x => x.Id == accountId && x.Status == "ACTIVE", cancellationToken);
        if (account is null) return null;

        var targetRole = await dbContext.Roles.SingleOrDefaultAsync(
            x => x.Id == roleId && x.Status == "ACTIVE", cancellationToken)
            ?? throw new ArgumentException("Nhóm quyền không tồn tại hoặc đã ngừng hoạt động.");
        var isSystemAdmin = targetRole.Code.Equals(AdminRoleCode, StringComparison.OrdinalIgnoreCase);

        if (account.Role.Code.Equals(AdminRoleCode, StringComparison.OrdinalIgnoreCase) && !isSystemAdmin)
        {
            var activeAdminCount = await dbContext.Accounts.CountAsync(
                x => x.RoleId == account.RoleId && x.Status == "ACTIVE", cancellationToken);
            if (activeAdminCount <= 1)
                throw new InvalidOperationException("Phải giữ lại ít nhất một tài khoản thuộc nhóm Quản trị viên.");
        }

        var activePermissionIds = await dbContext.Permissions
            .Where(x => x.Status == "ACTIVE")
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var activePermissionIdSet = activePermissionIds.ToHashSet();

        if (usesCustomPermissions && permissionIds.Any(x => !activePermissionIdSet.Contains(x)))
            throw new ArgumentException("Danh sách quyền có quyền không tồn tại hoặc đã ngừng hoạt động.");

        var selectedIds = isSystemAdmin
            ? activePermissionIds.Order().ToArray()
            : usesCustomPermissions
                ? permissionIds.Distinct().Order().ToArray()
                : await dbContext.RolePermissions
                    .Where(x => x.RoleId == roleId && activePermissionIdSet.Contains(x.PermissionId))
                    .Select(x => x.PermissionId)
                    .OrderBy(x => x)
                    .ToArrayAsync(cancellationToken);

        var currentGrants = await dbContext.AccountPermissions
            .Where(x => x.AccountId == accountId)
            .ToListAsync(cancellationToken);
        var storedIds = isSystemAdmin || !usesCustomPermissions ? [] : selectedIds;
        var storedIdSet = storedIds.ToHashSet();
        var currentPermissionIdSet = currentGrants.Select(x => x.PermissionId).ToHashSet();
        dbContext.AccountPermissions.RemoveRange(currentGrants.Where(x => !storedIdSet.Contains(x.PermissionId)));
        dbContext.AccountPermissions.AddRange(storedIds
            .Where(permissionId => !currentPermissionIdSet.Contains(permissionId))
            .Select(permissionId => new AccountPermissionEntity
            {
                AccountId = accountId,
                PermissionId = permissionId
            }));

        account.RoleId = roleId;
        account.UsesCustomPermissions = !isSystemAdmin && usesCustomPermissions;
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetAsync(cancellationToken)).Accounts.Single(x => x.Id == accountId);
    }

    private sealed class RolePermissionGrant
    {
        public int RoleId { get; init; }
        public int PermissionId { get; init; }
    }

    private sealed class AccountPermissionGrant
    {
        public int AccountId { get; init; }
        public int PermissionId { get; init; }
    }
}
