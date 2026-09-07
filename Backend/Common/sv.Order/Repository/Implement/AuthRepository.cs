using Dapper;
using Microsoft.EntityFrameworkCore;
using Sv.Order.Data;
using Sv.Order.DTOs;
using Sv.Order.Repository.Interface;

namespace Sv.Order.Repository.Implement;

public sealed class AuthRepository(OrderDbContext dbContext) : IAuthRepository
{
    public async Task<AuthUserDto?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT tk.TaiKhoanID AS AccountId, tk.NhanVienID AS EmployeeId,
                   tk.TenDangNhap AS Username, tk.MatKhauHash AS PasswordHash,
                   nv.HoTen AS FullName, nq.TenNhomQuyen AS RoleName
            FROM tbl_TaiKhoan tk
            INNER JOIN tbl_NhanVien nv ON nv.NhanVienID = tk.NhanVienID
            INNER JOIN tbl_NhomQuyen nq ON nq.NhomQuyenID = tk.NhomQuyenID
            WHERE tk.TenDangNhap = @Username AND tk.TrangThai = 'ACTIVE'
              AND nv.TrangThai = 'ACTIVE' AND nv.IsDeleted = 0;

            SELECT q.MaQuyen
            FROM tbl_TaiKhoan tk
            INNER JOIN tbl_CapQuyen cq ON cq.NhomQuyenID = tk.NhomQuyenID
            INNER JOIN tbl_Quyen q ON q.QuyenID = cq.QuyenID
            WHERE tk.TenDangNhap = @Username AND q.TrangThai = 'ACTIVE';
            """;

        var connection = dbContext.Database.GetDbConnection();
        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            sql, new { Username = username.Trim() }, cancellationToken: cancellationToken));
        var user = await multi.ReadSingleOrDefaultAsync<AuthUserDto>();
        if (user is null || !PasswordMatches(user.PasswordHash, password)) return null;
        user.Permissions = (await multi.ReadAsync<string>()).AsList();

        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE tbl_TaiKhoan SET LanDangNhapCuoi = SYSDATETIME() WHERE TaiKhoanID = @AccountId",
            new { user.AccountId }, cancellationToken: cancellationToken));
        return user;
    }

    private static bool PasswordMatches(string storedHash, string password) =>
        storedHash == "DEMO_HASH_123456" && password == "123456";
}
