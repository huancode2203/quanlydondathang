using Microsoft.EntityFrameworkCore;
using Sv.Order.Entities;

namespace Sv.Order.Data;

public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<OrderItemEntity> OrderItems => Set<OrderItemEntity>();
    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<RoleEntity> Roles => Set<RoleEntity>();
    public DbSet<PermissionEntity> Permissions => Set<PermissionEntity>();
    public DbSet<RolePermissionEntity> RolePermissions => Set<RolePermissionEntity>();
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<AccountPermissionEntity> AccountPermissions => Set<AccountPermissionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var order = modelBuilder.Entity<OrderEntity>();
        order.ToTable("tbl_DonDatHang");
        order.HasKey(x => x.Id);
        order.Property(x => x.Id).HasColumnName("DonDatHangID");
        order.Property(x => x.Code).HasColumnName("MaDonHang").HasMaxLength(30).IsUnicode(false);
        order.HasIndex(x => x.Code).IsUnique();
        order.Property(x => x.CustomerId).HasColumnName("KhachHangID");
        order.Property(x => x.ManagerEmployeeId).HasColumnName("NhanVienQuanLyID");
        order.Property(x => x.DeliveryEmployeeId).HasColumnName("NhanVienGiaoHangID");
        order.Property(x => x.OrderedAt).HasColumnName("NgayDatHang");
        order.Property(x => x.ExpectedDeliveryAt).HasColumnName("NgayGiaoDuKien");
        order.Property(x => x.DeliveredAt).HasColumnName("NgayGiaoThucTe");
        order.Property(x => x.DeliveryAddress).HasColumnName("DiaChiGiaoHang").HasMaxLength(500);
        order.Property(x => x.MerchandiseTotal).HasColumnName("TongTienHang").HasPrecision(18, 2);
        order.Property(x => x.DiscountAmount).HasColumnName("TienGiamGia").HasPrecision(18, 2);
        order.Property(x => x.TaxAmount).HasColumnName("TienThue").HasPrecision(18, 2);
        order.Property(x => x.ShippingFee).HasColumnName("PhiGiaoHang").HasPrecision(18, 2);
        order.Property(x => x.GrandTotal).HasColumnName("TongThanhToan").HasPrecision(18, 2).ValueGeneratedOnAddOrUpdate();
        order.Property(x => x.Status).HasColumnName("TrangThai").HasMaxLength(30).IsUnicode(false);
        order.Property(x => x.StockDeducted).HasColumnName("DaTruKho");
        order.Property(x => x.Note).HasColumnName("GhiChu").HasMaxLength(1000);
        order.Property(x => x.CreatedAt).HasColumnName("NgayTao").ValueGeneratedOnAdd();
        order.Property(x => x.UpdatedAt).HasColumnName("NgayCapNhat");

        var item = modelBuilder.Entity<OrderItemEntity>();
        item.ToTable("tbl_ChiTietDonDatHang", table => table.UseSqlOutputClause(false));
        item.HasKey(x => x.Id);
        item.Property(x => x.Id).HasColumnName("ChiTietDonDatHangID");
        item.Property(x => x.OrderId).HasColumnName("DonDatHangID");
        item.Property(x => x.ProductId).HasColumnName("HangHoaID");
        item.Property(x => x.Quantity).HasColumnName("SoLuong").HasPrecision(18, 2);
        item.Property(x => x.UnitPrice).HasColumnName("DonGia").HasPrecision(18, 2);
        item.Property(x => x.DiscountPercent).HasColumnName("PhanTramGiamGia").HasPrecision(5, 2);
        item.Property(x => x.LineTotal).HasColumnName("ThanhTien").HasPrecision(18, 2).ValueGeneratedOnAddOrUpdate();
        item.Property(x => x.Note).HasColumnName("GhiChu").HasMaxLength(500);
        item.HasIndex(x => new { x.OrderId, x.ProductId }).IsUnique();
        item.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);

        var customer = modelBuilder.Entity<CustomerEntity>();
        customer.ToTable("tbl_KhachHang");
        customer.HasKey(x => x.Id);
        customer.Property(x => x.Id).HasColumnName("KhachHangID");
        customer.Property(x => x.Code).HasColumnName("MaKhachHang").HasMaxLength(20).IsUnicode(false);
        customer.HasIndex(x => x.Code).IsUnique();
        customer.Property(x => x.Name).HasColumnName("TenKhachHang").HasMaxLength(200);
        customer.Property(x => x.Phone).HasColumnName("SoDienThoai").HasMaxLength(20).IsUnicode(false);
        customer.Property(x => x.Email).HasColumnName("Email").HasMaxLength(255).IsUnicode(false);
        customer.Property(x => x.Address).HasColumnName("DiaChi").HasMaxLength(500);
        customer.Property(x => x.TaxCode).HasColumnName("MaSoThue").HasMaxLength(50).IsUnicode(false);
        customer.Property(x => x.Note).HasColumnName("GhiChu").HasMaxLength(1000);
        customer.Property(x => x.Status).HasColumnName("TrangThai").HasMaxLength(20).IsUnicode(false);
        customer.Property(x => x.CreatedAt).HasColumnName("NgayTao").ValueGeneratedOnAdd();
        customer.Property(x => x.UpdatedAt).HasColumnName("NgayCapNhat");
        customer.Property(x => x.IsDeleted).HasColumnName("IsDeleted");

        var product = modelBuilder.Entity<ProductEntity>();
        product.ToTable("tbl_HangHoa");
        product.HasKey(x => x.Id);
        product.Property(x => x.Id).HasColumnName("HangHoaID");
        product.Property(x => x.Code).HasColumnName("MaHang").HasMaxLength(30).IsUnicode(false);
        product.HasIndex(x => x.Code).IsUnique();
        product.Property(x => x.Name).HasColumnName("TenHang").HasMaxLength(255);
        product.Property(x => x.Unit).HasColumnName("DonViTinh").HasMaxLength(50);
        product.Property(x => x.Price).HasColumnName("GiaBan").HasPrecision(18, 2);
        product.Property(x => x.StockQuantity).HasColumnName("SoLuongTon").HasPrecision(18, 2);
        product.Property(x => x.Description).HasColumnName("MoTa").HasMaxLength(1000);
        product.Property(x => x.Status).HasColumnName("TrangThai").HasMaxLength(20).IsUnicode(false);
        product.Property(x => x.CreatedAt).HasColumnName("NgayTao").ValueGeneratedOnAdd();
        product.Property(x => x.UpdatedAt).HasColumnName("NgayCapNhat");
        product.Property(x => x.IsDeleted).HasColumnName("IsDeleted");

        var role = modelBuilder.Entity<RoleEntity>();
        role.ToTable("tbl_NhomQuyen");
        role.HasKey(x => x.Id);
        role.Property(x => x.Id).HasColumnName("NhomQuyenID");
        role.Property(x => x.Code).HasColumnName("MaNhomQuyen").HasMaxLength(50).IsUnicode(false);
        role.Property(x => x.Name).HasColumnName("TenNhomQuyen").HasMaxLength(150);
        role.Property(x => x.Description).HasColumnName("MoTa").HasMaxLength(500);
        role.Property(x => x.Status).HasColumnName("TrangThai").HasMaxLength(20).IsUnicode(false);
        role.Property(x => x.CreatedAt).HasColumnName("NgayTao").ValueGeneratedOnAdd();

        var permission = modelBuilder.Entity<PermissionEntity>();
        permission.ToTable("tbl_Quyen");
        permission.HasKey(x => x.Id);
        permission.Property(x => x.Id).HasColumnName("QuyenID");
        permission.Property(x => x.Code).HasColumnName("MaQuyen").HasMaxLength(100).IsUnicode(false);
        permission.Property(x => x.Name).HasColumnName("TenQuyen").HasMaxLength(150);
        permission.Property(x => x.Feature).HasColumnName("ChucNang").HasMaxLength(100).IsUnicode(false);
        permission.Property(x => x.Action).HasColumnName("HanhDong").HasMaxLength(50).IsUnicode(false);
        permission.Property(x => x.Description).HasColumnName("MoTa").HasMaxLength(500);
        permission.Property(x => x.Status).HasColumnName("TrangThai").HasMaxLength(20).IsUnicode(false);
        permission.Property(x => x.CreatedAt).HasColumnName("NgayTao").ValueGeneratedOnAdd();

        var rolePermission = modelBuilder.Entity<RolePermissionEntity>();
        rolePermission.ToTable("tbl_CapQuyen");
        rolePermission.HasKey(x => x.Id);
        rolePermission.Property(x => x.Id).HasColumnName("CapQuyenID");
        rolePermission.Property(x => x.RoleId).HasColumnName("NhomQuyenID");
        rolePermission.Property(x => x.PermissionId).HasColumnName("QuyenID");
        rolePermission.Property(x => x.GrantedAt).HasColumnName("NgayCap").ValueGeneratedOnAdd();
        rolePermission.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
        rolePermission.HasOne(x => x.Role).WithMany(x => x.Permissions).HasForeignKey(x => x.RoleId);
        rolePermission.HasOne(x => x.Permission).WithMany(x => x.Roles).HasForeignKey(x => x.PermissionId);

        var account = modelBuilder.Entity<AccountEntity>();
        account.ToTable("tbl_TaiKhoan");
        account.HasKey(x => x.Id);
        account.Property(x => x.Id).HasColumnName("TaiKhoanID");
        account.Property(x => x.EmployeeId).HasColumnName("NhanVienID");
        account.Property(x => x.RoleId).HasColumnName("NhomQuyenID");
        account.Property(x => x.Username).HasColumnName("TenDangNhap").HasMaxLength(100).IsUnicode(false);
        account.Property(x => x.Status).HasColumnName("TrangThai").HasMaxLength(20).IsUnicode(false);
        account.Property(x => x.UsesCustomPermissions).HasColumnName("SuDungQuyenRieng");
        account.HasOne(x => x.Role).WithMany(x => x.Accounts).HasForeignKey(x => x.RoleId);

        var accountPermission = modelBuilder.Entity<AccountPermissionEntity>();
        accountPermission.ToTable("tbl_CapQuyenTaiKhoan");
        accountPermission.HasKey(x => x.Id);
        accountPermission.Property(x => x.Id).HasColumnName("CapQuyenTaiKhoanID");
        accountPermission.Property(x => x.AccountId).HasColumnName("TaiKhoanID");
        accountPermission.Property(x => x.PermissionId).HasColumnName("QuyenID");
        accountPermission.Property(x => x.GrantedAt).HasColumnName("NgayCap").ValueGeneratedOnAdd();
        accountPermission.HasIndex(x => new { x.AccountId, x.PermissionId }).IsUnique();
        accountPermission.HasOne(x => x.Account).WithMany(x => x.Permissions).HasForeignKey(x => x.AccountId);
        accountPermission.HasOne(x => x.Permission).WithMany(x => x.Accounts).HasForeignKey(x => x.PermissionId);
    }
}
