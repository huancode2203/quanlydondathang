USE QuanLyDonDatHangDB;
GO

IF COL_LENGTH('dbo.tbl_HangHoa', 'SoLuongTon') IS NULL
BEGIN
    EXEC(N'ALTER TABLE dbo.tbl_HangHoa
        ADD SoLuongTon DECIMAL(18,2) NOT NULL
            CONSTRAINT DF_HangHoa_SoLuongTon DEFAULT 0');

    EXEC(N'ALTER TABLE dbo.tbl_HangHoa
        ADD CONSTRAINT CK_HangHoa_SoLuongTon CHECK (SoLuongTon >= 0)');
END;
GO

UPDATE dbo.tbl_HangHoa
SET SoLuongTon = CASE MaHang
    WHEN 'HH001' THEN 12
    WHEN 'HH002' THEN 80
    WHEN 'HH003' THEN 35
    WHEN 'HH004' THEN 20
    WHEN 'HH005' THEN 45
    ELSE SoLuongTon
END
WHERE MaHang BETWEEN 'HH001' AND 'HH005';
GO
