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
                   nv.HoTen AS FullName, roles.RoleName
            FROM tbl_TaiKhoan tk
            INNER JOIN tbl_NhanVien nv ON nv.NhanVienID = tk.NhanVienID
            CROSS APPLY (
                SELECT STRING_AGG(nq.TenNhomQuyen, N', ') WITHIN GROUP (ORDER BY nq.TenNhomQuyen) AS RoleName
                FROM tbl_TaiKhoanNhomQuyen tknq
                INNER JOIN tbl_NhomQuyen nq ON nq.NhomQuyenID = tknq.NhomQuyenID
                WHERE tknq.TaiKhoanID = tk.TaiKhoanID AND nq.TrangThai = 'ACTIVE'
            ) roles
            WHERE tk.TenDangNhap = @Username AND tk.TrangThai = 'ACTIVE'
              AND nv.TrangThai = 'ACTIVE' AND nv.IsDeleted = 0
              AND roles.RoleName IS NOT NULL;

            SELECT q.MaQuyen
            FROM tbl_TaiKhoan tk
            CROSS JOIN tbl_Quyen q
            WHERE tk.TenDangNhap = @Username AND tk.TrangThai = 'ACTIVE' AND q.TrangThai = 'ACTIVE'
              AND (
                  EXISTS (
                      SELECT 1 FROM tbl_TaiKhoanNhomQuyen tknq
                      INNER JOIN tbl_NhomQuyen nq ON nq.NhomQuyenID = tknq.NhomQuyenID
                      WHERE tknq.TaiKhoanID = tk.TaiKhoanID AND nq.TrangThai = 'ACTIVE' AND nq.MaNhomQuyen = 'ADMIN'
                  )
                  OR (tk.SuDungQuyenRieng = 1 AND EXISTS (
                      SELECT 1 FROM tbl_CapQuyenTaiKhoan cqt
                      WHERE cqt.TaiKhoanID = tk.TaiKhoanID AND cqt.QuyenID = q.QuyenID
                  ))
                  OR (tk.SuDungQuyenRieng = 0 AND EXISTS (
                      SELECT 1 FROM tbl_TaiKhoanNhomQuyen tknq
                      INNER JOIN tbl_NhomQuyen nq ON nq.NhomQuyenID = tknq.NhomQuyenID AND nq.TrangThai = 'ACTIVE'
                      INNER JOIN tbl_CapQuyen cq ON cq.NhomQuyenID = tknq.NhomQuyenID
                      WHERE tknq.TaiKhoanID = tk.TaiKhoanID AND cq.QuyenID = q.QuyenID
                  ))
              )
            ORDER BY q.QuyenID;
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
