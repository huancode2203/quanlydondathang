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

IF COL_LENGTH('dbo.tbl_DonDatHang', 'DaTruKho') IS NULL
BEGIN
    ALTER TABLE dbo.tbl_DonDatHang
        ADD DaTruKho BIT NOT NULL
            CONSTRAINT DF_DonDatHang_DaTruKho DEFAULT 0;
END
GO

-- Dữ liệu lịch sử được giữ nguyên vì không thể xác định tồn kho đã được
-- điều chỉnh thủ công hay chưa. Các đơn chuyển sang DA_GIAO từ phiên bản
-- này trở đi sẽ được API đánh dấu DaTruKho = 1 sau khi trừ kho thành công.
