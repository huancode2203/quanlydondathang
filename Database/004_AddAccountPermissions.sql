USE QuanLyDonDatHangDB;
GO

IF COL_LENGTH('dbo.tbl_TaiKhoan', 'SuDungQuyenRieng') IS NULL
BEGIN
    ALTER TABLE dbo.tbl_TaiKhoan
    ADD SuDungQuyenRieng BIT NOT NULL
        CONSTRAINT DF_TaiKhoan_SuDungQuyenRieng DEFAULT 0;
END;
GO

IF OBJECT_ID('dbo.tbl_CapQuyenTaiKhoan', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_CapQuyenTaiKhoan (
        CapQuyenTaiKhoanID INT IDENTITY(1,1) PRIMARY KEY,
        TaiKhoanID         INT NOT NULL,
        QuyenID            INT NOT NULL,
        NgayCap            DATETIME2 NOT NULL
            CONSTRAINT DF_CapQuyenTaiKhoan_NgayCap DEFAULT SYSDATETIME(),
        CONSTRAINT FK_CapQuyenTaiKhoan_TaiKhoan
            FOREIGN KEY (TaiKhoanID) REFERENCES dbo.tbl_TaiKhoan(TaiKhoanID),
        CONSTRAINT FK_CapQuyenTaiKhoan_Quyen
            FOREIGN KEY (QuyenID) REFERENCES dbo.tbl_Quyen(QuyenID),
        CONSTRAINT UQ_CapQuyenTaiKhoan UNIQUE (TaiKhoanID, QuyenID)
    );
END;
GO
