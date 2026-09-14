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
            FROM tbl_NhomQuyen WHERE TrangThai = 'ACTIVE'
            ORDER BY CASE WHEN MaNhomQuyen = 'ADMIN' THEN 0 ELSE 1 END, TenNhomQuyen;

            SELECT QuyenID AS Id, MaQuyen AS Code, TenQuyen AS Name,
                   ISNULL(ChucNang, 'OTHER') AS Feature, ISNULL(HanhDong, '') AS Action, MoTa AS Description
            FROM tbl_Quyen WHERE TrangThai = 'ACTIVE' ORDER BY ChucNang, QuyenID;

            SELECT NhomQuyenID AS RoleId, QuyenID AS PermissionId FROM tbl_CapQuyen;

            SELECT tk.TaiKhoanID AS Id, tk.TenDangNhap AS Username, nv.HoTen AS FullName,
                   tk.SuDungQuyenRieng AS UsesCustomPermissions
            FROM tbl_TaiKhoan tk
            INNER JOIN tbl_NhanVien nv ON nv.NhanVienID = tk.NhanVienID
            WHERE tk.TrangThai = 'ACTIVE' AND nv.TrangThai = 'ACTIVE' AND nv.IsDeleted = 0;

            SELECT tknq.TaiKhoanID AS AccountId, tknq.NhomQuyenID AS RoleId
            FROM tbl_TaiKhoanNhomQuyen tknq
            INNER JOIN tbl_NhomQuyen nq ON nq.NhomQuyenID = tknq.NhomQuyenID
            WHERE nq.TrangThai = 'ACTIVE';

            SELECT TaiKhoanID AS AccountId, QuyenID AS PermissionId FROM tbl_CapQuyenTaiKhoan;
            """;

        var connection = dbContext.Database.GetDbConnection();
        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
        var roles = (await multi.ReadAsync<RolePermissionDto>()).AsList();
        var permissions = (await multi.ReadAsync<PermissionDto>()).AsList();
        var roleGrants = (await multi.ReadAsync<RolePermissionGrant>()).AsList();
        var accounts = (await multi.ReadAsync<AccountPermissionDto>()).AsList();
        var accountRoles = (await multi.ReadAsync<AccountRoleGrant>()).AsList();
        var accountGrants = (await multi.ReadAsync<AccountPermissionGrant>()).AsList();
        var activePermissionIds = permissions.Select(x => x.Id).ToHashSet();

        foreach (var role in roles)
        {
            role.PermissionIds = role.IsSystemAdmin
                ? activePermissionIds.Order().ToArray()
                : roleGrants.Where(x => x.RoleId == role.Id && activePermissionIds.Contains(x.PermissionId))
                    .Select(x => x.PermissionId).Distinct().Order().ToArray();
        }

        var roleLookup = roles.ToDictionary(x => x.Id);
        foreach (var account in accounts)
        {
            account.RoleIds = accountRoles.Where(x => x.AccountId == account.Id && roleLookup.ContainsKey(x.RoleId))
                .Select(x => x.RoleId).Distinct().Order().ToArray();
            var assignedRoles = account.RoleIds.Select(id => roleLookup[id]).ToArray();
            account.RoleNames = assignedRoles.Select(x => x.Name).ToArray();
            account.IsSystemAdmin = assignedRoles.Any(x => x.IsSystemAdmin);
            account.PermissionIds = account.IsSystemAdmin
                ? activePermissionIds.Order().ToArray()
                : account.UsesCustomPermissions
                    ? accountGrants.Where(x => x.AccountId == account.Id && activePermissionIds.Contains(x.PermissionId))
                        .Select(x => x.PermissionId).Distinct().Order().ToArray()
                    : assignedRoles.SelectMany(x => x.PermissionIds).Distinct().Order().ToArray();
        }

        var sortedAccounts = accounts.OrderByDescending(x => x.IsSystemAdmin).ThenBy(x => x.FullName).ToArray();
        return new PermissionManagementDto(roles, permissions, sortedAccounts);
    }

    public async Task<RolePermissionDto?> UpdateRoleAsync(int roleId, IReadOnlyCollection<int> permissionIds, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles.SingleOrDefaultAsync(x => x.Id == roleId && x.Status == "ACTIVE", cancellationToken);
        if (role is null) return null;

        var activePermissions = await dbContext.Permissions.Where(x => x.Status == "ACTIVE").ToListAsync(cancellationToken);
        var isSystemAdmin = role.Code.Equals(AdminRoleCode, StringComparison.OrdinalIgnoreCase);
        var selectedIds = isSystemAdmin
            ? activePermissions.Select(x => x.Id).Order().ToArray()
            : NormalizePermissionIds(activePermissions, permissionIds);

        var currentGrants = await dbContext.RolePermissions.Where(x => x.RoleId == roleId).ToListAsync(cancellationToken);
        SynchronizeRolePermissions(currentGrants, selectedIds, roleId);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RolePermissionDto
        {
            Id = role.Id, Code = role.Code, Name = role.Name, Description = role.Description,
            IsSystemAdmin = isSystemAdmin, PermissionIds = selectedIds
        };
    }

    public async Task<AccountPermissionDto?> UpdateAccountAsync(
        int accountId, IReadOnlyCollection<int> roleIds, bool usesCustomPermissions,
        IReadOnlyCollection<int> permissionIds, CancellationToken cancellationToken)
    {
        var account = await dbContext.Accounts.SingleOrDefaultAsync(x => x.Id == accountId && x.Status == "ACTIVE", cancellationToken);
        if (account is null) return null;

        var selectedRoleIds = roleIds.Distinct().Order().ToArray();
        if (selectedRoleIds.Length == 0) throw new ArgumentException("Tài khoản phải thuộc ít nhất một nhóm quyền.");

        var targetRoles = await dbContext.Roles.Where(x => selectedRoleIds.Contains(x.Id) && x.Status == "ACTIVE").ToListAsync(cancellationToken);
        if (targetRoles.Count != selectedRoleIds.Length)
            throw new ArgumentException("Danh sách nhóm quyền có nhóm không tồn tại hoặc đã ngừng hoạt động.");

        var adminRole = await dbContext.Roles.SingleAsync(x => x.Code == AdminRoleCode && x.Status == "ACTIVE", cancellationToken);
        var isSystemAdmin = selectedRoleIds.Contains(adminRole.Id);
        var currentIsAdmin = await dbContext.AccountRoles.AnyAsync(x => x.AccountId == accountId && x.RoleId == adminRole.Id, cancellationToken);
        if (currentIsAdmin && !isSystemAdmin)
        {
            var activeAdminCount = await dbContext.AccountRoles.Where(x => x.RoleId == adminRole.Id)
                .Join(dbContext.Accounts.Where(x => x.Status == "ACTIVE"), x => x.AccountId, x => x.Id, (_, item) => item.Id)
                .Distinct().CountAsync(cancellationToken);
            if (activeAdminCount <= 1)
                throw new InvalidOperationException("Phải giữ lại ít nhất một tài khoản thuộc nhóm Quản trị viên.");
        }

        var activePermissions = await dbContext.Permissions.Where(x => x.Status == "ACTIVE").ToListAsync(cancellationToken);
        int[] selectedPermissionIds;
        if (isSystemAdmin) selectedPermissionIds = activePermissions.Select(x => x.Id).Order().ToArray();
        else if (usesCustomPermissions) selectedPermissionIds = NormalizePermissionIds(activePermissions, permissionIds);
        else selectedPermissionIds = await dbContext.RolePermissions.Where(x => selectedRoleIds.Contains(x.RoleId))
            .Select(x => x.PermissionId).Distinct().OrderBy(x => x).ToArrayAsync(cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var currentRoles = await dbContext.AccountRoles.Where(x => x.AccountId == accountId).ToListAsync(cancellationToken);
        var roleIdSet = selectedRoleIds.ToHashSet();
        var currentRoleIdSet = currentRoles.Select(x => x.RoleId).ToHashSet();
        dbContext.AccountRoles.RemoveRange(currentRoles.Where(x => !roleIdSet.Contains(x.RoleId)));
        dbContext.AccountRoles.AddRange(selectedRoleIds.Where(x => !currentRoleIdSet.Contains(x)).Select(roleId => new AccountRoleEntity
        {
            AccountId = accountId, RoleId = roleId
        }));

        var currentGrants = await dbContext.AccountPermissions.Where(x => x.AccountId == accountId).ToListAsync(cancellationToken);
        var storedIds = isSystemAdmin || !usesCustomPermissions ? [] : selectedPermissionIds;
        var storedIdSet = storedIds.ToHashSet();
        var currentPermissionIdSet = currentGrants.Select(x => x.PermissionId).ToHashSet();
        dbContext.AccountPermissions.RemoveRange(currentGrants.Where(x => !storedIdSet.Contains(x.PermissionId)));
        dbContext.AccountPermissions.AddRange(storedIds.Where(x => !currentPermissionIdSet.Contains(x)).Select(permissionId => new AccountPermissionEntity
        {
            AccountId = accountId, PermissionId = permissionId
        }));

        account.RoleId = isSystemAdmin ? adminRole.Id : selectedRoleIds[0];
        account.UsesCustomPermissions = !isSystemAdmin && usesCustomPermissions;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (await GetAsync(cancellationToken)).Accounts.Single(x => x.Id == accountId);
    }

    private void SynchronizeRolePermissions(List<RolePermissionEntity> current, IReadOnlyCollection<int> selectedIds, int roleId)
    {
        var selected = selectedIds.ToHashSet();
        var existing = current.Select(x => x.PermissionId).ToHashSet();
        dbContext.RolePermissions.RemoveRange(current.Where(x => !selected.Contains(x.PermissionId)));
        dbContext.RolePermissions.AddRange(selectedIds.Where(x => !existing.Contains(x)).Select(permissionId => new RolePermissionEntity
        {
            RoleId = roleId, PermissionId = permissionId
        }));
    }

    private static int[] NormalizePermissionIds(IReadOnlyCollection<PermissionEntity> activePermissions, IReadOnlyCollection<int> requestedIds)
    {
        var activeIds = activePermissions.Select(x => x.Id).ToHashSet();
        if (requestedIds.Any(x => !activeIds.Contains(x)))
            throw new ArgumentException("Danh sách quyền có quyền không tồn tại hoặc đã ngừng hoạt động.");

        var selected = requestedIds.Distinct().ToHashSet();
        foreach (var feature in activePermissions.GroupBy(x => x.Feature))
        {
            var view = feature.FirstOrDefault(x => string.Equals(x.Action, "VIEW", StringComparison.OrdinalIgnoreCase));
            if (view is null) continue;
            if (feature.Any(x => !string.Equals(x.Action, "VIEW", StringComparison.OrdinalIgnoreCase) && selected.Contains(x.Id)))
                selected.Add(view.Id);
        }
        return selected.Order().ToArray();
    }

    private sealed class RolePermissionGrant { public int RoleId { get; init; } public int PermissionId { get; init; } }
    private sealed class AccountRoleGrant { public int AccountId { get; init; } public int RoleId { get; init; } }
    private sealed class AccountPermissionGrant { public int AccountId { get; init; } public int PermissionId { get; init; } }
}
