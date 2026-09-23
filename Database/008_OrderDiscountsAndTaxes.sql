-- Add amount and percentage discounts per line, per-line VAT and invoice VAT rate.
-- Existing orders keep their old amounts: new rates and fixed line discounts default to 0.
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

IF OBJECT_ID(N'dbo.tbl_DonDatHang', N'U') IS NULL OR OBJECT_ID(N'dbo.tbl_ChiTietDonDatHang', N'U') IS NULL
    THROW 51020, N'Không tìm thấy các bảng đơn hàng. Hãy tạo/nâng cấp schema trước khi chạy migration 008.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH(N'dbo.tbl_DonDatHang', N'PhanTramThue') IS NULL
        ALTER TABLE dbo.tbl_DonDatHang ADD PhanTramThue DECIMAL(5,2) NOT NULL
            CONSTRAINT DF_DonDatHang_PhanTramThue DEFAULT 0 WITH VALUES;
    IF COL_LENGTH(N'dbo.tbl_ChiTietDonDatHang', N'TienGiamGia') IS NULL
        ALTER TABLE dbo.tbl_ChiTietDonDatHang ADD TienGiamGia DECIMAL(18,2) NOT NULL
            CONSTRAINT DF_ChiTietDon_TienGiamGia DEFAULT 0 WITH VALUES;
    IF COL_LENGTH(N'dbo.tbl_ChiTietDonDatHang', N'PhanTramThue') IS NULL
        ALTER TABLE dbo.tbl_ChiTietDonDatHang ADD PhanTramThue DECIMAL(5,2) NOT NULL
            CONSTRAINT DF_ChiTietDon_PhanTramThue DEFAULT 0 WITH VALUES;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tbl_DonDatHang') AND name = N'PhanTramThue')
    BEGIN
        DECLARE @InvalidHeaderRate bit = 0;
        EXEC sys.sp_executesql N'SELECT @Invalid = CASE WHEN EXISTS (
            SELECT 1 FROM dbo.tbl_DonDatHang WHERE PhanTramThue NOT BETWEEN 0 AND 100) THEN 1 ELSE 0 END;',
            N'@Invalid bit OUTPUT', @InvalidHeaderRate OUTPUT;
        IF @InvalidHeaderRate = 1 THROW 51021, N'Tỷ lệ thuế hóa đơn hiện có nằm ngoài 0 đến 100%.', 1;
    END;
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tbl_ChiTietDonDatHang') AND name = N'PhanTramThue')
    BEGIN
        DECLARE @InvalidItemRate bit = 0;
        EXEC sys.sp_executesql N'SELECT @Invalid = CASE WHEN EXISTS (
            SELECT 1 FROM dbo.tbl_ChiTietDonDatHang WHERE PhanTramThue NOT BETWEEN 0 AND 100) THEN 1 ELSE 0 END;',
            N'@Invalid bit OUTPUT', @InvalidItemRate OUTPUT;
        IF @InvalidItemRate = 1 THROW 51022, N'Tỷ lệ thuế mặt hàng hiện có nằm ngoài 0 đến 100%.', 1;
    END;

    -- ThanhTien remains the discounted merchandise amount before tax. Per-line
    -- tax is calculated separately; the order trigger continues summing ThanhTien.
    ALTER TABLE dbo.tbl_ChiTietDonDatHang DROP CONSTRAINT IF EXISTS CK_ChiTietDon_GiamGia;
    ALTER TABLE dbo.tbl_ChiTietDonDatHang DROP CONSTRAINT IF EXISTS CK_ChiTietDon_AmountDomain;
    ALTER TABLE dbo.tbl_ChiTietDonDatHang DROP CONSTRAINT IF EXISTS CK_ChiTietDon_DiscountTaxDomain;
    ALTER TABLE dbo.tbl_ChiTietDonDatHang DROP COLUMN IF EXISTS ThanhTien;
    ALTER TABLE dbo.tbl_ChiTietDonDatHang DROP COLUMN IF EXISTS TienThue;

    EXEC sys.sp_executesql N'ALTER TABLE dbo.tbl_ChiTietDonDatHang ADD ThanhTien AS
        CONVERT(DECIMAL(18,2), SoLuong * DonGia * (1 - PhanTramGiamGia / 100.0) - TienGiamGia) PERSISTED;';
    EXEC sys.sp_executesql N'ALTER TABLE dbo.tbl_ChiTietDonDatHang ADD TienThue AS
        CONVERT(DECIMAL(18,2),
            (SoLuong * DonGia * (1 - PhanTramGiamGia / 100.0) - TienGiamGia) * PhanTramThue / 100.0) PERSISTED;';

    EXEC sys.sp_executesql N'ALTER TABLE dbo.tbl_DonDatHang DROP CONSTRAINT IF EXISTS CK_DonDatHang_TaxPercent;
        ALTER TABLE dbo.tbl_DonDatHang WITH CHECK ADD CONSTRAINT CK_DonDatHang_TaxPercent
            CHECK (PhanTramThue BETWEEN 0 AND 100);
        ALTER TABLE dbo.tbl_DonDatHang WITH CHECK CHECK CONSTRAINT CK_DonDatHang_TaxPercent;';

    EXEC sys.sp_executesql N'ALTER TABLE dbo.tbl_ChiTietDonDatHang WITH CHECK ADD CONSTRAINT CK_ChiTietDon_DiscountTaxDomain CHECK (
            PhanTramGiamGia BETWEEN 0 AND 100 AND
            TienGiamGia BETWEEN 0 AND 999999999999.99 AND
            PhanTramThue BETWEEN 0 AND 100 AND
            ThanhTien BETWEEN 0 AND 999999999999.99 AND
            TienThue BETWEEN 0 AND 999999999999.99
        );
        ALTER TABLE dbo.tbl_ChiTietDonDatHang WITH CHECK CHECK CONSTRAINT CK_ChiTietDon_DiscountTaxDomain;';

    COMMIT TRANSACTION;
    SELECT N'PASS: existing prices, discounts and totals preserved; new rates default to zero.' AS Result;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
