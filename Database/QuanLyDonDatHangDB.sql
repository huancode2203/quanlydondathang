-- ============================================================
-- TẠO DATABASE
-- ============================================================
USE master;
GO

CREATE DATABASE QuanLyDonDatHangDB
COLLATE Vietnamese_CI_AS;
GO

USE QuanLyDonDatHangDB;
GO

-- ============================================================
-- I. NHÂN VIÊN
-- ============================================================
CREATE TABLE tbl_NhanVien (
    NhanVienID      INT IDENTITY(1,1) PRIMARY KEY,
    MaNhanVien      VARCHAR(20) NOT NULL UNIQUE,
    HoTen           NVARCHAR(150) NOT NULL,
    NgaySinh        DATE NULL,
    GioiTinh        NVARCHAR(10) NULL,
    SoDienThoai     VARCHAR(20) NULL,
    Email           VARCHAR(255) NULL,
    DiaChi          NVARCHAR(500) NULL,
    ChucVu          NVARCHAR(100) NULL,
    NgayVaoLam      DATE NULL,
    TrangThai       VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    NgayTao         DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    NgayCapNhat     DATETIME2 NULL,
    IsDeleted       BIT NOT NULL DEFAULT 0,
    CONSTRAINT CK_NhanVien_GioiTinh
        CHECK (GioiTinh IS NULL OR GioiTinh IN (N'Nam', N'Nữ', N'Khác')),
    CONSTRAINT CK_NhanVien_TrangThai
        CHECK (TrangThai IN ('ACTIVE', 'INACTIVE'))
);
GO

-- ============================================================
-- II. HỆ THỐNG PHÂN QUYỀN
-- ============================================================
CREATE TABLE tbl_NhomQuyen (
    NhomQuyenID     INT IDENTITY(1,1) PRIMARY KEY,
    MaNhomQuyen     VARCHAR(50) NOT NULL UNIQUE,
    TenNhomQuyen    NVARCHAR(150) NOT NULL,
    MoTa            NVARCHAR(500) NULL,
    TrangThai       VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    NgayTao         DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT CK_NhomQuyen_TrangThai
        CHECK (TrangThai IN ('ACTIVE', 'INACTIVE'))
);
GO

CREATE TABLE tbl_Quyen (
    QuyenID         INT IDENTITY(1,1) PRIMARY KEY,
    MaQuyen         VARCHAR(100) NOT NULL UNIQUE,
    TenQuyen        NVARCHAR(150) NOT NULL,
    ChucNang        VARCHAR(100) NULL,
    HanhDong        VARCHAR(50) NULL,
    MoTa            NVARCHAR(500) NULL,
    TrangThai       VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    NgayTao         DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT CK_Quyen_TrangThai
        CHECK (TrangThai IN ('ACTIVE', 'INACTIVE'))
);
GO

CREATE TABLE tbl_CapQuyen (
    CapQuyenID      INT IDENTITY(1,1) PRIMARY KEY,
    NhomQuyenID     INT NOT NULL,
    QuyenID         INT NOT NULL,
    NgayCap         DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY (NhomQuyenID) REFERENCES tbl_NhomQuyen(NhomQuyenID),
    FOREIGN KEY (QuyenID) REFERENCES tbl_Quyen(QuyenID),
    CONSTRAINT UQ_CapQuyen UNIQUE (NhomQuyenID, QuyenID)
);
GO

CREATE TABLE tbl_TaiKhoan (
    TaiKhoanID      INT IDENTITY(1,1) PRIMARY KEY,
    NhanVienID      INT NOT NULL UNIQUE,
    NhomQuyenID     INT NOT NULL,
    TenDangNhap     VARCHAR(100) NOT NULL UNIQUE,
    MatKhauHash     VARCHAR(500) NOT NULL,
    TrangThai       VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    SuDungQuyenRieng BIT NOT NULL DEFAULT 0,
    LanDangNhapCuoi DATETIME2 NULL,
    NgayTao         DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    NgayCapNhat     DATETIME2 NULL,
    FOREIGN KEY (NhanVienID) REFERENCES tbl_NhanVien(NhanVienID),
    FOREIGN KEY (NhomQuyenID) REFERENCES tbl_NhomQuyen(NhomQuyenID),
    CONSTRAINT CK_TaiKhoan_TrangThai
        CHECK (TrangThai IN ('ACTIVE', 'INACTIVE', 'LOCKED'))
);
GO

CREATE TABLE tbl_CapQuyenTaiKhoan (
    CapQuyenTaiKhoanID INT IDENTITY(1,1) PRIMARY KEY,
    TaiKhoanID         INT NOT NULL,
    QuyenID            INT NOT NULL,
    NgayCap            DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY (TaiKhoanID) REFERENCES tbl_TaiKhoan(TaiKhoanID),
    FOREIGN KEY (QuyenID) REFERENCES tbl_Quyen(QuyenID),
    CONSTRAINT UQ_CapQuyenTaiKhoan UNIQUE (TaiKhoanID, QuyenID)
);
GO

-- ============================================================
-- III. KHÁCH HÀNG
-- ============================================================
CREATE TABLE tbl_KhachHang (
    KhachHangID     INT IDENTITY(1,1) PRIMARY KEY,
    MaKhachHang     VARCHAR(20) NOT NULL UNIQUE,
    TenKhachHang    NVARCHAR(200) NOT NULL,
    SoDienThoai     VARCHAR(20) NULL,
    Email           VARCHAR(255) NULL,
    DiaChi          NVARCHAR(500) NULL,
    MaSoThue        VARCHAR(50) NULL,
    GhiChu          NVARCHAR(1000) NULL,
    TrangThai       VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    NgayTao         DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    NgayCapNhat     DATETIME2 NULL,
    IsDeleted       BIT NOT NULL DEFAULT 0,
    CONSTRAINT CK_KhachHang_TrangThai
        CHECK (TrangThai IN ('ACTIVE', 'INACTIVE'))
);
GO

-- ============================================================
-- IV. HÀNG HÓA
-- ============================================================
CREATE TABLE tbl_HangHoa (
    HangHoaID       INT IDENTITY(1,1) PRIMARY KEY,
    MaHang          VARCHAR(30) NOT NULL UNIQUE,
    TenHang         NVARCHAR(255) NOT NULL,
    DonViTinh       NVARCHAR(50) NOT NULL,
    GiaBan          DECIMAL(18,2) NOT NULL DEFAULT 0,
    SoLuongTon      DECIMAL(18,2) NOT NULL DEFAULT 0,
    MoTa            NVARCHAR(1000) NULL,
    TrangThai       VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    NgayTao         DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    NgayCapNhat     DATETIME2 NULL,
    IsDeleted       BIT NOT NULL DEFAULT 0,
    CONSTRAINT CK_HangHoa_GiaBan CHECK (GiaBan >= 0),
    CONSTRAINT CK_HangHoa_SoLuongTon CHECK (SoLuongTon >= 0),
    CONSTRAINT CK_HangHoa_TrangThai
        CHECK (TrangThai IN ('ACTIVE', 'INACTIVE'))
);
GO

-- ============================================================
-- V. ĐƠN ĐẶT HÀNG
-- ============================================================
CREATE TABLE tbl_DonDatHang (
    DonDatHangID        BIGINT IDENTITY(1,1) PRIMARY KEY,
    MaDonHang           VARCHAR(30) NOT NULL UNIQUE,
    KhachHangID         INT NOT NULL,
    NhanVienQuanLyID    INT NOT NULL,
    NhanVienGiaoHangID  INT NULL,
    NgayDatHang         DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    NgayGiaoDuKien      DATETIME2 NULL,
    NgayGiaoThucTe      DATETIME2 NULL,
    DiaChiGiaoHang      NVARCHAR(500) NOT NULL,
    TongTienHang        DECIMAL(18,2) NOT NULL DEFAULT 0,
    TienGiamGia         DECIMAL(18,2) NOT NULL DEFAULT 0,
    TienThue            DECIMAL(18,2) NOT NULL DEFAULT 0,
    PhiGiaoHang         DECIMAL(18,2) NOT NULL DEFAULT 0,
    TongThanhToan AS
        CONVERT(DECIMAL(18,2), TongTienHang - TienGiamGia + TienThue + PhiGiaoHang) PERSISTED,
    TrangThai           VARCHAR(30) NOT NULL DEFAULT 'CHO_XAC_NHAN',
    GhiChu              NVARCHAR(1000) NULL,
    NgayTao             DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    NgayCapNhat         DATETIME2 NULL,
    FOREIGN KEY (KhachHangID) REFERENCES tbl_KhachHang(KhachHangID),
    FOREIGN KEY (NhanVienQuanLyID) REFERENCES tbl_NhanVien(NhanVienID),
    FOREIGN KEY (NhanVienGiaoHangID) REFERENCES tbl_NhanVien(NhanVienID),
    CONSTRAINT CK_DonDatHang_TongTien    CHECK (TongTienHang >= 0),
    CONSTRAINT CK_DonDatHang_GiamGia     CHECK (TienGiamGia >= 0),
    CONSTRAINT CK_DonDatHang_Thue        CHECK (TienThue >= 0),
    CONSTRAINT CK_DonDatHang_PhiGiao     CHECK (PhiGiaoHang >= 0),
    CONSTRAINT CK_DonDatHang_TrangThai
        CHECK (TrangThai IN (
            'CHO_XAC_NHAN', 'DA_XAC_NHAN', 'DANG_CHUAN_BI',
            'CHO_GIAO_HANG', 'DANG_GIAO', 'DA_GIAO', 'DA_HUY'
        ))
);
GO

-- ============================================================
-- VI. CHI TIẾT ĐƠN ĐẶT HÀNG
-- ============================================================
CREATE TABLE tbl_ChiTietDonDatHang (
    ChiTietDonDatHangID BIGINT IDENTITY(1,1) PRIMARY KEY,
    DonDatHangID        BIGINT NOT NULL,
    HangHoaID           INT NOT NULL,
    SoLuong             DECIMAL(18,2) NOT NULL,
    DonGia              DECIMAL(18,2) NOT NULL,
    PhanTramGiamGia     DECIMAL(5,2) NOT NULL DEFAULT 0,
    ThanhTien AS
        CONVERT(DECIMAL(18,2), SoLuong * DonGia * (1 - PhanTramGiamGia / 100.0)) PERSISTED,
    GhiChu              NVARCHAR(500) NULL,
    FOREIGN KEY (DonDatHangID) REFERENCES tbl_DonDatHang(DonDatHangID) ON DELETE CASCADE,
    FOREIGN KEY (HangHoaID) REFERENCES tbl_HangHoa(HangHoaID),
    CONSTRAINT CK_ChiTietDon_SoLuong CHECK (SoLuong > 0),
    CONSTRAINT CK_ChiTietDon_DonGia  CHECK (DonGia >= 0),
    CONSTRAINT CK_ChiTietDon_GiamGia CHECK (PhanTramGiamGia BETWEEN 0 AND 100),
    CONSTRAINT UQ_ChiTietDon_HangHoa  UNIQUE (DonDatHangID, HangHoaID)
);
GO

-- ============================================================
-- VIII. INDEX
-- ============================================================
CREATE INDEX IX_NhanVien_HoTen ON tbl_NhanVien(HoTen);
GO

CREATE INDEX IX_TaiKhoan_NhomQuyen ON tbl_TaiKhoan(NhomQuyenID);
GO

CREATE INDEX IX_CapQuyen_NhomQuyen ON tbl_CapQuyen(NhomQuyenID);
CREATE INDEX IX_CapQuyen_Quyen ON tbl_CapQuyen(QuyenID);
GO

CREATE INDEX IX_KhachHang_Ten ON tbl_KhachHang(TenKhachHang);
CREATE INDEX IX_KhachHang_SoDienThoai ON tbl_KhachHang(SoDienThoai);
GO

CREATE INDEX IX_HangHoa_Ten ON tbl_HangHoa(TenHang);
GO

CREATE INDEX IX_DonDatHang_KhachHang ON tbl_DonDatHang(KhachHangID);
CREATE INDEX IX_DonDatHang_NhanVienQuanLy ON tbl_DonDatHang(NhanVienQuanLyID);
CREATE INDEX IX_DonDatHang_NhanVienGiaoHang ON tbl_DonDatHang(NhanVienGiaoHangID);
CREATE INDEX IX_DonDatHang_TrangThai ON tbl_DonDatHang(TrangThai);
CREATE INDEX IX_DonDatHang_NgayDat ON tbl_DonDatHang(NgayDatHang);
GO

CREATE INDEX IX_ChiTietDon_DonDatHang ON tbl_ChiTietDonDatHang(DonDatHangID);
GO
GO

-- ============================================================
-- IX. TRIGGER
-- ============================================================
CREATE TRIGGER trg_CapNhatTongTienDonHang
ON tbl_ChiTietDonDatHang
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    WITH DonBiAnhHuong AS (
        SELECT DonDatHangID FROM inserted
        UNION
        SELECT DonDatHangID FROM deleted
    )
    UPDATE D
    SET TongTienHang = (
            SELECT ISNULL(SUM(CT.ThanhTien), 0)
            FROM tbl_ChiTietDonDatHang CT
            WHERE CT.DonDatHangID = D.DonDatHangID
        ),
        NgayCapNhat = SYSDATETIME()
    FROM tbl_DonDatHang D
    INNER JOIN DonBiAnhHuong A ON D.DonDatHangID = A.DonDatHangID;
END
GO

-- ============================================================
-- X. FUNCTION
-- ============================================================
CREATE FUNCTION fn_TinhTongTienHang (@DonDatHangID BIGINT)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @TongTien DECIMAL(18,2);
    SELECT @TongTien = SUM(ThanhTien)
    FROM tbl_ChiTietDonDatHang
    WHERE DonDatHangID = @DonDatHangID;
    RETURN ISNULL(@TongTien, 0);
END
GO

-- ============================================================
-- XI. DỮ LIỆU MẪU
-- ============================================================

-- Nhóm quyền
INSERT INTO tbl_NhomQuyen (MaNhomQuyen, TenNhomQuyen, MoTa)
VALUES
    ('SALE_STAFF', N'Nhân viên bán hàng', N'Nhân viên quản lý và tạo đơn đặt hàng'),
    ('DELIVERY_STAFF', N'Nhân viên giao hàng', N'Nhân viên thực hiện giao hàng'),
    ('SALE_MANAGER', N'Quản lý bán hàng', N'Quản lý hoạt động đặt hàng'),
    ('ADMIN', N'Quản trị viên', N'Quản trị hệ thống');
GO

-- Quyền
INSERT INTO tbl_Quyen (MaQuyen, TenQuyen, ChucNang, HanhDong, MoTa)
VALUES
    ('ORDER_VIEW', N'Xem đơn đặt hàng', 'ORDER', 'VIEW', N'Cho phép xem đơn hàng'),
    ('ORDER_CREATE', N'Tạo đơn đặt hàng', 'ORDER', 'CREATE', N'Cho phép tạo đơn hàng'),
    ('ORDER_UPDATE', N'Cập nhật đơn đặt hàng', 'ORDER', 'UPDATE', N'Cho phép sửa đơn hàng'),
    ('ORDER_DELETE', N'Xóa đơn đặt hàng', 'ORDER', 'DELETE', N'Cho phép xóa đơn hàng'),
    ('ORDER_APPROVE', N'Duyệt đơn đặt hàng', 'ORDER', 'APPROVE', N'Cho phép duyệt đơn hàng'),
    ('ORDER_DELIVERY', N'Giao đơn hàng', 'ORDER', 'DELIVERY', N'Cho phép cập nhật trạng thái giao hàng'),
    ('CUSTOMER_VIEW', N'Xem khách hàng', 'CUSTOMER', 'VIEW', N'Cho phép xem khách hàng'),
    ('CUSTOMER_MANAGE', N'Quản lý khách hàng', 'CUSTOMER', 'MANAGE', N'Cho phép quản lý khách hàng'),
    ('PRODUCT_VIEW', N'Xem hàng hóa', 'PRODUCT', 'VIEW', N'Cho phép xem hàng hóa'),
    ('PRODUCT_MANAGE', N'Quản lý hàng hóa', 'PRODUCT', 'MANAGE', N'Cho phép quản lý hàng hóa'),
    ('ACCOUNT_MANAGE', N'Quản lý tài khoản', 'ACCOUNT', 'MANAGE', N'Cho phép quản lý tài khoản'),
    ('PERMISSION_MANAGE', N'Phân quyền', 'PERMISSION', 'MANAGE', N'Cho phép cấu hình quyền');
GO

-- Cấp quyền cho nhóm SALE_STAFF
INSERT INTO tbl_CapQuyen (NhomQuyenID, QuyenID)
VALUES
    (1, 1), (1, 2), (1, 3), (1, 7), (1, 8), (1, 9);
GO

-- Cấp quyền cho nhóm DELIVERY_STAFF
INSERT INTO tbl_CapQuyen (NhomQuyenID, QuyenID)
VALUES
    (2, 1), (2, 6), (2, 7);
GO

-- Cấp quyền cho nhóm SALE_MANAGER
INSERT INTO tbl_CapQuyen (NhomQuyenID, QuyenID)
VALUES
    (3, 1), (3, 2), (3, 3), (3, 4), (3, 5), (3, 6),
    (3, 7), (3, 8), (3, 9), (3, 10);
GO

-- Admin có tất cả quyền
INSERT INTO tbl_CapQuyen (NhomQuyenID, QuyenID)
SELECT 4, QuyenID FROM tbl_Quyen;
GO

-- Nhân viên
INSERT INTO tbl_NhanVien (MaNhanVien, HoTen, NgaySinh, GioiTinh, SoDienThoai, Email, DiaChi, ChucVu, NgayVaoLam)
VALUES
    ('NV001', N'Nguyễn Văn An', '1995-05-10', N'Nam', '0901111111', 'an@company.com', N'TP.HCM', N'Nhân viên bán hàng', '2025-01-01'),
    ('NV002', N'Trần Thị Bình', '1992-08-15', N'Nữ', '0902222222', 'binh@company.com', N'TP.HCM', N'Quản lý bán hàng', '2023-01-01'),
    ('NV003', N'Lê Văn Cường', '1997-11-20', N'Nam', '0903333333', 'cuong@company.com', N'TP.HCM', N'Nhân viên giao hàng', '2025-03-01'),
    ('NV004', N'Phạm Minh Admin', '1990-01-01', N'Nam', '0904444444', 'admin@company.com', N'TP.HCM', N'Quản trị viên', '2022-01-01');
GO

-- Tài khoản
INSERT INTO tbl_TaiKhoan (NhanVienID, NhomQuyenID, TenDangNhap, MatKhauHash)
VALUES
    (1, 1, 'sale01', 'DEMO_HASH_123456'),
    (2, 3, 'manager01', 'DEMO_HASH_123456'),
    (3, 2, 'delivery01', 'DEMO_HASH_123456'),
    (4, 4, 'admin', 'DEMO_HASH_123456');
GO

-- Khách hàng
INSERT INTO tbl_KhachHang (MaKhachHang, TenKhachHang, SoDienThoai, Email, DiaChi)
VALUES
    ('KH001', N'Nguyễn Hoàng Minh', '0911111111', 'minh@gmail.com', N'Quận 1, TP.HCM'),
    ('KH002', N'Trần Thị Lan', '0922222222', 'lan@gmail.com', N'Bình Thạnh, TP.HCM'),
    ('KH003', N'Phạm Văn Nam', '0933333333', 'nam@gmail.com', N'Thủ Đức, TP.HCM');
GO

-- Hàng hóa
INSERT INTO tbl_HangHoa (MaHang, TenHang, DonViTinh, GiaBan, SoLuongTon, MoTa)
VALUES
    ('HH001', N'Laptop Dell Inspiron', N'Cái', 20000000, 12, N'Laptop Dell Inspiron'),
    ('HH002', N'Chuột Logitech', N'Cái', 500000, 80, N'Chuột không dây Logitech'),
    ('HH003', N'Bàn phím cơ', N'Cái', 1200000, 35, N'Bàn phím cơ'),
    ('HH004', N'Màn hình 24 inch', N'Cái', 4000000, 20, N'Màn hình máy tính'),
    ('HH005', N'Tai nghe Bluetooth', N'Cái', 800000, 45, N'Tai nghe không dây');
GO

-- Đơn đặt hàng
INSERT INTO tbl_DonDatHang (
    MaDonHang, KhachHangID, NhanVienQuanLyID, NhanVienGiaoHangID,
    NgayDatHang, NgayGiaoDuKien, DiaChiGiaoHang,
    TienGiamGia, TienThue, PhiGiaoHang, TrangThai, GhiChu
)
VALUES
    (
        'DH001', 1, 1, 3,
        SYSDATETIME(), DATEADD(DAY, 2, SYSDATETIME()),
        N'Quận 1, TP.HCM',
        500000, 0, 30000,
        'DA_XAC_NHAN', N'Giao hàng trong giờ hành chính'
    ),
    (
        'DH002', 2, 1, NULL,
        SYSDATETIME(), DATEADD(DAY, 3, SYSDATETIME()),
        N'Bình Thạnh, TP.HCM',
        0, 0, 30000,
        'CHO_XAC_NHAN', NULL
    );
GO

-- Chi tiết đơn
INSERT INTO tbl_ChiTietDonDatHang (DonDatHangID, HangHoaID, SoLuong, DonGia, PhanTramGiamGia)
VALUES
    (1, 1, 1, 20000000, 0),
    (1, 2, 2, 500000, 0),
    (1, 3, 1, 1200000, 10),
    (2, 4, 2, 4000000, 0),
    (2, 5, 1, 800000, 0);
GO

GO

-- ============================================================
-- XII. KIỂM TRA
-- ============================================================
SELECT * FROM tbl_NhanVien;
SELECT * FROM tbl_TaiKhoan;
SELECT * FROM tbl_NhomQuyen;
SELECT * FROM tbl_Quyen;
SELECT * FROM tbl_CapQuyen;
SELECT * FROM tbl_KhachHang;
SELECT * FROM tbl_HangHoa;
SELECT * FROM tbl_DonDatHang;
SELECT * FROM tbl_ChiTietDonDatHang;
GO
