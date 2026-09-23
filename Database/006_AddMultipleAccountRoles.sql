SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

USE QuanLyDonDatHangDB;
GO

IF OBJECT_ID('dbo.tbl_TaiKhoanNhomQuyen', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_TaiKhoanNhomQuyen (
        TaiKhoanNhomQuyenID BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_TaiKhoanNhomQuyen PRIMARY KEY,
        TaiKhoanID INT NOT NULL,
        NhomQuyenID INT NOT NULL,
        NgayGan DATETIME2 NOT NULL
            CONSTRAINT DF_TaiKhoanNhomQuyen_NgayGan DEFAULT SYSDATETIME(),
        CONSTRAINT FK_TaiKhoanNhomQuyen_TaiKhoan FOREIGN KEY (TaiKhoanID)
            REFERENCES dbo.tbl_TaiKhoan(TaiKhoanID) ON DELETE CASCADE,
        CONSTRAINT FK_TaiKhoanNhomQuyen_NhomQuyen FOREIGN KEY (NhomQuyenID)
            REFERENCES dbo.tbl_NhomQuyen(NhomQuyenID),
        CONSTRAINT UQ_TaiKhoanNhomQuyen UNIQUE (TaiKhoanID, NhomQuyenID)
    );

    CREATE INDEX IX_TaiKhoanNhomQuyen_NhomQuyen
        ON dbo.tbl_TaiKhoanNhomQuyen(NhomQuyenID, TaiKhoanID);
END
GO

-- Migration 007 removes the legacy column; re-running 006 stays safe.
IF COL_LENGTH('dbo.tbl_TaiKhoan', 'NhomQuyenID') IS NOT NULL
    EXEC sys.sp_executesql N'
        INSERT INTO dbo.tbl_TaiKhoanNhomQuyen (TaiKhoanID, NhomQuyenID)
        SELECT tk.TaiKhoanID, tk.NhomQuyenID
        FROM dbo.tbl_TaiKhoan tk
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.tbl_TaiKhoanNhomQuyen tknq
            WHERE tknq.TaiKhoanID = tk.TaiKhoanID
              AND tknq.NhomQuyenID = tk.NhomQuyenID
        );';
GO

-- Nếu một nhóm có quyền thao tác thì luôn bổ sung quyền VIEW của cùng phân hệ.
INSERT INTO dbo.tbl_CapQuyen (NhomQuyenID, QuyenID)
SELECT DISTINCT cq.NhomQuyenID, qView.QuyenID
FROM dbo.tbl_CapQuyen cq
INNER JOIN dbo.tbl_Quyen qAction ON qAction.QuyenID = cq.QuyenID
INNER JOIN dbo.tbl_Quyen qView ON qView.ChucNang = qAction.ChucNang
    AND qView.HanhDong = 'VIEW' AND qView.TrangThai = 'ACTIVE'
WHERE ISNULL(qAction.HanhDong, '') <> 'VIEW'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.tbl_CapQuyen existing
      WHERE existing.NhomQuyenID = cq.NhomQuyenID
        AND existing.QuyenID = qView.QuyenID
  );
GO

-- Quyền riêng của tài khoản cũng tuân theo quy tắc VIEW là quyền nền.
INSERT INTO dbo.tbl_CapQuyenTaiKhoan (TaiKhoanID, QuyenID)
SELECT DISTINCT cqt.TaiKhoanID, qView.QuyenID
FROM dbo.tbl_CapQuyenTaiKhoan cqt
INNER JOIN dbo.tbl_Quyen qAction ON qAction.QuyenID = cqt.QuyenID
INNER JOIN dbo.tbl_Quyen qView ON qView.ChucNang = qAction.ChucNang
    AND qView.HanhDong = 'VIEW' AND qView.TrangThai = 'ACTIVE'
WHERE ISNULL(qAction.HanhDong, '') <> 'VIEW'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.tbl_CapQuyenTaiKhoan existing
      WHERE existing.TaiKhoanID = cqt.TaiKhoanID
        AND existing.QuyenID = qView.QuyenID
  );
GO
