-- sqlcmd -S SERVER -E -C -b -i Database/007_NormalizeDomains.sql -v ApplyChanges=0
-- ApplyChanges=0: read-only preflight. ApplyChanges=1: apply after a database backup.
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
SET NOCOUNT ON;
GO
USE QuanLyDonDatHangDB;
GO

DECLARE @Apply bit = $(ApplyChanges);
DECLARE @Rules TABLE (TableName sysname, ConstraintName sysname, Predicate nvarchar(max));
INSERT INTO @Rules VALUES
    (N'tbl_NhanVien', N'CK_NhanVien_Domain', N'LEN(LTRIM(RTRIM(MaNhanVien))) > 0 AND LEN(LTRIM(RTRIM(HoTen))) > 0 AND (NgaySinh IS NULL OR NgayVaoLam IS NULL OR NgaySinh <= NgayVaoLam) AND (IsDeleted = 0 OR TrangThai = ''INACTIVE'')'),
    (N'tbl_NhomQuyen', N'CK_NhomQuyen_Domain', N'LEN(LTRIM(RTRIM(MaNhomQuyen))) > 0 AND LEN(LTRIM(RTRIM(TenNhomQuyen))) > 0'),
    (N'tbl_Quyen', N'CK_Quyen_Domain', N'ChucNang IS NOT NULL AND HanhDong IS NOT NULL AND LEN(LTRIM(RTRIM(TenQuyen))) > 0 AND MaQuyen = ChucNang + ''_'' + HanhDong AND ChucNang IN (''ORDER'',''CUSTOMER'',''PRODUCT'',''ACCOUNT'',''PERMISSION'') AND HanhDong IN (''VIEW'',''CREATE'',''UPDATE'',''DELETE'',''APPROVE'',''DELIVERY'',''MANAGE'')'),
    (N'tbl_TaiKhoan', N'CK_TaiKhoan_Domain', N'LEN(LTRIM(RTRIM(TenDangNhap))) > 0 AND LEN(LTRIM(RTRIM(MatKhauHash))) > 0'),
    (N'tbl_KhachHang', N'CK_KhachHang_Domain', N'LEN(LTRIM(RTRIM(MaKhachHang))) > 0 AND LEN(LTRIM(RTRIM(TenKhachHang))) > 0 AND (IsDeleted = 0 OR TrangThai = ''INACTIVE'')'),
    (N'tbl_HangHoa', N'CK_HangHoa_Domain', N'LEN(LTRIM(RTRIM(MaHang))) > 0 AND LEN(LTRIM(RTRIM(TenHang))) > 0 AND LEN(LTRIM(RTRIM(DonViTinh))) > 0 AND GiaBan BETWEEN 0 AND 999999999999.99 AND SoLuongTon BETWEEN 0 AND 999999999.99 AND (IsDeleted = 0 OR TrangThai = ''INACTIVE'')'),
    (N'tbl_DonDatHang', N'CK_DonDatHang_DateDomain', N'NgayGiaoDuKien IS NOT NULL AND NgayGiaoDuKien > NgayDatHang AND ((TrangThai = ''DA_GIAO'' AND NgayGiaoThucTe IS NOT NULL AND NgayGiaoThucTe >= NgayDatHang) OR (TrangThai <> ''DA_GIAO'' AND NgayGiaoThucTe IS NULL))'),
    (N'tbl_DonDatHang', N'CK_DonDatHang_AmountDomain', N'TongTienHang BETWEEN 0 AND 999999999999.99 AND TienGiamGia BETWEEN 0 AND 999999999999.99 AND TienThue BETWEEN 0 AND 999999999999.99 AND PhiGiaoHang BETWEEN 0 AND 999999999999.99 AND TongThanhToan BETWEEN 0 AND 999999999999.99'),
    (N'tbl_DonDatHang', N'CK_DonDatHang_Domain', N'LEN(LTRIM(RTRIM(MaDonHang))) > 0 AND LEN(LTRIM(RTRIM(DiaChiGiaoHang))) > 0 AND (DaTruKho = 0 OR TrangThai = ''DA_GIAO'') AND (TrangThai NOT IN (''CHO_GIAO_HANG'',''DANG_GIAO'',''DA_GIAO'') OR NhanVienGiaoHangID IS NOT NULL)'),
    (N'tbl_ChiTietDonDatHang', N'CK_ChiTietDon_AmountDomain', N'SoLuong BETWEEN 0.01 AND 999999999.99 AND DonGia BETWEEN 0 AND 999999999999.99 AND ThanhTien BETWEEN 0 AND 999999999999.99');

IF OBJECT_ID(N'dbo.tbl_TaiKhoanNhomQuyen', N'U') IS NULL
    THROW 51000, N'Phải chạy migration 006 trước migration 007.', 1;

DECLARE @Table sysname, @Constraint sysname, @Predicate nvarchar(max), @Sql nvarchar(max), @Bad bigint;
DECLARE @Issues TABLE (RuleName nvarchar(200), InvalidRows bigint);
DECLARE rules_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT TableName, ConstraintName, Predicate FROM @Rules;
OPEN rules_cursor;
FETCH NEXT FROM rules_cursor INTO @Table, @Constraint, @Predicate;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Sql = N'SELECT @Bad = COUNT_BIG(*) FROM dbo.' + QUOTENAME(@Table) + N' WHERE NOT (' + @Predicate + N');';
    EXEC sys.sp_executesql @Sql, N'@Bad bigint OUTPUT', @Bad OUTPUT;
    IF @Bad > 0 INSERT INTO @Issues VALUES (@Constraint, @Bad);
    FETCH NEXT FROM rules_cursor INTO @Table, @Constraint, @Predicate;
END;
CLOSE rules_cursor;
DEALLOCATE rules_cursor;

-- Every legacy membership must already exist in the many-to-many relation.
IF COL_LENGTH(N'dbo.tbl_TaiKhoan', N'NhomQuyenID') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'SELECT @Bad = COUNT_BIG(*) FROM dbo.tbl_TaiKhoan a
        WHERE NOT EXISTS (SELECT 1 FROM dbo.tbl_TaiKhoanNhomQuyen r
            WHERE r.TaiKhoanID = a.TaiKhoanID AND r.NhomQuyenID = a.NhomQuyenID);',
        N'@Bad bigint OUTPUT', @Bad OUTPUT;
    IF @Bad > 0 INSERT INTO @Issues VALUES (N'Legacy role membership missing from join table', @Bad);
    IF EXISTS (
        SELECT 1 FROM sys.sql_expression_dependencies d
        JOIN sys.objects o ON o.object_id = d.referencing_id
        WHERE d.referenced_id = OBJECT_ID(N'dbo.tbl_TaiKhoan')
          AND o.type IN ('P','V','FN','IF','TF','TR')
    )
        INSERT INTO @Issues VALUES (N'Review SQL module dependencies on tbl_TaiKhoan before dropping legacy column', 1);
    IF EXISTS (
        SELECT 1 FROM sys.index_columns ic JOIN sys.indexes i
          ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        WHERE ic.object_id = OBJECT_ID(N'dbo.tbl_TaiKhoan')
          AND ic.column_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.tbl_TaiKhoan'), N'NhomQuyenID', 'ColumnId')
          AND i.name <> N'IX_TaiKhoan_NhomQuyen'
    )
        INSERT INTO @Issues VALUES (N'Unexpected index depends on legacy account role column', 1);
END;

IF EXISTS (SELECT 1 FROM sys.sql_expression_dependencies WHERE referenced_id = OBJECT_ID(N'dbo.fn_TinhTongTienHang'))
    INSERT INTO @Issues VALUES (N'Function fn_TinhTongTienHang is referenced by another database object', 1);

INSERT INTO @Issues
SELECT N'Active account without an active group', COUNT_BIG(*)
FROM dbo.tbl_TaiKhoan a
WHERE a.TrangThai = 'ACTIVE' AND NOT EXISTS (
    SELECT 1 FROM dbo.tbl_TaiKhoanNhomQuyen ar
    JOIN dbo.tbl_NhomQuyen r ON r.NhomQuyenID = ar.NhomQuyenID AND r.TrangThai = 'ACTIVE'
    WHERE ar.TaiKhoanID = a.TaiKhoanID)
HAVING COUNT_BIG(*) > 0;

SELECT RuleName, InvalidRows FROM @Issues;
IF EXISTS (SELECT 1 FROM @Issues)
    THROW 51001, N'Dữ liệu hoặc dependency chưa đạt miền giá trị; migration chưa thay đổi dữ liệu. Kiểm tra báo cáo trước khi tiếp tục.', 1;
IF @Apply = 0
BEGIN
    SELECT N'PASS: preflight only; no schema/data changes.' AS Result;
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    -- CHECK WITH CHECK validates existing rows again inside the transaction.
    DECLARE apply_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT TableName, ConstraintName, Predicate FROM @Rules;
    OPEN apply_cursor;
    FETCH NEXT FROM apply_cursor INTO @Table, @Constraint, @Predicate;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF OBJECT_ID(N'dbo.' + @Constraint, N'C') IS NULL
        BEGIN
            SET @Sql = N'ALTER TABLE dbo.' + QUOTENAME(@Table) + N' WITH CHECK ADD CONSTRAINT '
                + QUOTENAME(@Constraint) + N' CHECK (' + @Predicate + N');';
            EXEC sys.sp_executesql @Sql;
        END;
        SET @Sql = N'ALTER TABLE dbo.' + QUOTENAME(@Table) + N' WITH CHECK CHECK CONSTRAINT ' + QUOTENAME(@Constraint) + N';';
        EXEC sys.sp_executesql @Sql;
        FETCH NEXT FROM apply_cursor INTO @Table, @Constraint, @Predicate;
    END;
    CLOSE apply_cursor;
    DEALLOCATE apply_cursor;

    ALTER TABLE dbo.tbl_DonDatHang ALTER COLUMN NgayGiaoDuKien datetime2 NOT NULL;
    ALTER TABLE dbo.tbl_Quyen ALTER COLUMN ChucNang varchar(100) NOT NULL;
    ALTER TABLE dbo.tbl_Quyen ALTER COLUMN HanhDong varchar(50) NOT NULL;

    IF COL_LENGTH(N'dbo.tbl_TaiKhoan', N'NhomQuyenID') IS NOT NULL
    BEGIN
        -- Remove only FK/defaults bound to this exact obsolete column.
        SET @Sql = N'';
        SELECT @Sql += N'ALTER TABLE dbo.tbl_TaiKhoan DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';'
        FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
        WHERE fkc.parent_object_id = OBJECT_ID(N'dbo.tbl_TaiKhoan')
          AND fkc.parent_column_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.tbl_TaiKhoan'), N'NhomQuyenID', 'ColumnId');
        SELECT @Sql += N'ALTER TABLE dbo.tbl_TaiKhoan DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';'
        FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'dbo.tbl_TaiKhoan')
          AND dc.parent_column_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.tbl_TaiKhoan'), N'NhomQuyenID', 'ColumnId');
        IF LEN(@Sql) > 0 EXEC sys.sp_executesql @Sql;
        DROP INDEX IF EXISTS IX_TaiKhoan_NhomQuyen ON dbo.tbl_TaiKhoan;
        EXEC sys.sp_executesql N'ALTER TABLE dbo.tbl_TaiKhoan DROP COLUMN NhomQuyenID;';
    END;

    -- This duplicate scalar SUM was never used by application SQL or database objects.
    DROP FUNCTION IF EXISTS dbo.fn_TinhTongTienHang;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.tbl_ChiTietDonDatHang') AND name = N'IX_ChiTietDon_HangHoa_DonHang')
        CREATE INDEX IX_ChiTietDon_HangHoa_DonHang ON dbo.tbl_ChiTietDonDatHang(HangHoaID, DonDatHangID) INCLUDE (SoLuong);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.tbl_TaiKhoanNhomQuyen') AND name = N'IX_TaiKhoanNhomQuyen_NhomQuyen')
        CREATE INDEX IX_TaiKhoanNhomQuyen_NhomQuyen ON dbo.tbl_TaiKhoanNhomQuyen(NhomQuyenID, TaiKhoanID);

    COMMIT TRANSACTION;
    SELECT N'PASS: migration 007 applied. Existing order quantities and stock were not recalculated.' AS Result;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
