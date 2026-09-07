using Dapper;
using Microsoft.EntityFrameworkCore;
using Sv.Order.Data;
using Sv.Order.DTOs;
using Sv.Order.Entities;
using Sv.Order.Repository.Interface;

namespace Sv.Order.Repository.Implement;

public sealed class OrderRepository(OrderDbContext dbContext) : IOrderRepository
{
    private static readonly string[] ValidStatuses =
    [
        "CHO_XAC_NHAN", "DA_XAC_NHAN", "DANG_CHUAN_BI", "CHO_GIAO_HANG",
        "DANG_GIAO", "DA_GIAO", "DA_HUY"
    ];

    public async Task<PagedResult<OrderListItemDto>> SearchAsync(OrderSearchRequest request, CancellationToken cancellationToken)
    {
        if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate.Value.Date > request.ToDate.Value.Date)
            throw new ArgumentException("Từ ngày không được lớn hơn đến ngày.");
        if (request.MinTotal < 0 || request.MaxTotal < 0)
            throw new ArgumentException("Khoảng tổng tiền không được âm.");
        if (request.MinTotal.HasValue && request.MaxTotal.HasValue && request.MinTotal > request.MaxTotal)
            throw new ArgumentException("Tổng tiền nhỏ nhất không được lớn hơn tổng tiền lớn nhất.");
        var conditions = new List<string> { "1 = 1" };
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            conditions.Add("(d.MaDonHang LIKE @Keyword OR k.TenKhachHang LIKE @Keyword OR k.SoDienThoai LIKE @Keyword)");
            parameters.Add("Keyword", $"%{request.Keyword.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            conditions.Add("d.TrangThai = @Status");
            parameters.Add("Status", request.Status);
        }
        if (request.FromDate.HasValue)
        {
            conditions.Add("d.NgayDatHang >= @FromDate");
            parameters.Add("FromDate", request.FromDate.Value.Date);
        }
        if (request.ToDate.HasValue)
        {
            conditions.Add("d.NgayDatHang < DATEADD(DAY, 1, @ToDate)");
            parameters.Add("ToDate", request.ToDate.Value.Date);
        }
        if (request.CustomerId.HasValue)
        {
            conditions.Add("d.KhachHangID = @CustomerId");
            parameters.Add("CustomerId", request.CustomerId);
        }
        if (request.CreatorEmployeeId.HasValue)
        {
            conditions.Add("d.NhanVienQuanLyID = @CreatorEmployeeId");
            parameters.Add("CreatorEmployeeId", request.CreatorEmployeeId);
        }
        if (request.DeliveryEmployeeId.HasValue)
        {
            conditions.Add("d.NhanVienGiaoHangID = @DeliveryEmployeeId");
            parameters.Add("DeliveryEmployeeId", request.DeliveryEmployeeId);
        }
        if (request.MinTotal.HasValue)
        {
            conditions.Add("d.TongThanhToan >= @MinTotal");
            parameters.Add("MinTotal", request.MinTotal);
        }
        if (request.MaxTotal.HasValue)
        {
            conditions.Add("d.TongThanhToan <= @MaxTotal");
            parameters.Add("MaxTotal", request.MaxTotal);
        }

        parameters.Add("Offset", (request.Page - 1) * request.PageSize);
        parameters.Add("PageSize", request.PageSize);
        var where = string.Join(" AND ", conditions);
        var orderBy = request.Sort switch
        {
            "deliveryDateAsc" => "d.NgayGiaoDuKien ASC, d.DonDatHangID DESC",
            "deliveryDateDesc" => "d.NgayGiaoDuKien DESC, d.DonDatHangID DESC",
            "totalAsc" => "d.TongThanhToan ASC, d.DonDatHangID DESC",
            "totalDesc" => "d.TongThanhToan DESC, d.DonDatHangID DESC",
            _ => "d.NgayDatHang DESC, d.DonDatHangID DESC"
        };
        var sql = $$"""
            SELECT COUNT(1)
            FROM tbl_DonDatHang d
            INNER JOIN tbl_KhachHang k ON k.KhachHangID = d.KhachHangID
            WHERE {{where}};

            SELECT d.DonDatHangID AS Id, d.MaDonHang AS Code, d.KhachHangID AS CustomerId,
                   k.TenKhachHang AS CustomerName, k.SoDienThoai AS CustomerPhone,
                   nvql.HoTen AS CreatorName, nvg.HoTen AS DeliveryEmployeeName,
                   d.NgayDatHang AS OrderedAt, d.NgayGiaoDuKien AS ExpectedDeliveryAt,
                   d.DiaChiGiaoHang AS DeliveryAddress, d.TongThanhToan AS GrandTotal,
                   d.TrangThai AS Status,
                   (SELECT COUNT(1) FROM tbl_ChiTietDonDatHang ct WHERE ct.DonDatHangID = d.DonDatHangID) AS ItemCount
            FROM tbl_DonDatHang d
            INNER JOIN tbl_KhachHang k ON k.KhachHangID = d.KhachHangID
            INNER JOIN tbl_NhanVien nvql ON nvql.NhanVienID = d.NhanVienQuanLyID
            LEFT JOIN tbl_NhanVien nvg ON nvg.NhanVienID = d.NhanVienGiaoHangID
            WHERE {{where}}
            ORDER BY {{orderBy}}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var connection = dbContext.Database.GetDbConnection();
        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        var total = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<OrderListItemDto>()).AsList();
        return new PagedResult<OrderListItemDto>(items, total, request.Page, request.PageSize);
    }

    public async Task<OrderDetailDto?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT d.DonDatHangID AS Id, d.MaDonHang AS Code, d.KhachHangID AS CustomerId,
                   k.TenKhachHang AS CustomerName, k.SoDienThoai AS CustomerPhone,
                   d.NhanVienQuanLyID AS CreatorEmployeeId, nvql.HoTen AS CreatorName,
                   d.NhanVienGiaoHangID AS DeliveryEmployeeId, nvg.HoTen AS DeliveryEmployeeName,
                   d.NgayDatHang AS OrderedAt, d.NgayGiaoDuKien AS ExpectedDeliveryAt,
                   d.NgayGiaoThucTe AS DeliveredAt, d.DiaChiGiaoHang AS DeliveryAddress,
                   d.TongTienHang AS MerchandiseTotal, d.TienGiamGia AS DiscountAmount,
                   d.TienThue AS TaxAmount, d.PhiGiaoHang AS ShippingFee,
                   d.TongThanhToan AS GrandTotal, d.TrangThai AS Status, d.GhiChu AS Note,
                   (SELECT COUNT(1) FROM tbl_ChiTietDonDatHang x WHERE x.DonDatHangID = d.DonDatHangID) AS ItemCount
            FROM tbl_DonDatHang d
            INNER JOIN tbl_KhachHang k ON k.KhachHangID = d.KhachHangID
            INNER JOIN tbl_NhanVien nvql ON nvql.NhanVienID = d.NhanVienQuanLyID
            LEFT JOIN tbl_NhanVien nvg ON nvg.NhanVienID = d.NhanVienGiaoHangID
            WHERE d.DonDatHangID = @Id;

            SELECT ct.ChiTietDonDatHangID AS Id, ct.HangHoaID AS ProductId,
                   h.MaHang AS ProductCode, h.TenHang AS ProductName, h.DonViTinh AS Unit,
                   ct.SoLuong AS Quantity, ct.DonGia AS UnitPrice,
                   ct.PhanTramGiamGia AS DiscountPercent, ct.ThanhTien AS LineTotal,
                   h.SoLuongTon AS StockQuantity, ct.GhiChu AS Note
            FROM tbl_ChiTietDonDatHang ct
            INNER JOIN tbl_HangHoa h ON h.HangHoaID = ct.HangHoaID
            WHERE ct.DonDatHangID = @Id
            ORDER BY ct.ChiTietDonDatHangID;
            """;

        var connection = dbContext.Database.GetDbConnection();
        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
        var order = await multi.ReadSingleOrDefaultAsync<OrderDetailDto>();
        if (order is null) return null;
        order.Items = (await multi.ReadAsync<OrderItemDto>()).AsList();
        return order;
    }

    public async Task<OrderLookupsDto> GetLookupsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT KhachHangID AS Id, MaKhachHang AS Code, TenKhachHang AS Name, SoDienThoai AS Extra
            FROM tbl_KhachHang WHERE IsDeleted = 0 AND TrangThai = 'ACTIVE' ORDER BY TenKhachHang;
            SELECT HangHoaID AS Id, MaHang AS Code, TenHang AS Name, DonViTinh AS Extra,
                   GiaBan AS Price, SoLuongTon AS StockQuantity
            FROM tbl_HangHoa WHERE IsDeleted = 0 AND TrangThai = 'ACTIVE' ORDER BY TenHang;
            SELECT NhanVienID AS Id, MaNhanVien AS Code, HoTen AS Name, ChucVu AS Extra
            FROM tbl_NhanVien WHERE IsDeleted = 0 AND TrangThai = 'ACTIVE' ORDER BY HoTen;
            """;
        var connection = dbContext.Database.GetDbConnection();
        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
        var customers = (await multi.ReadAsync<LookupDto>()).AsList();
        var products = (await multi.ReadAsync<LookupDto>()).AsList();
        var employees = (await multi.ReadAsync<LookupDto>()).AsList();
        return new OrderLookupsDto(customers, products, employees);
    }

    public async Task<OrderDetailDto> CreateAsync(SaveOrderRequest request, int creatorEmployeeId, CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        if (await dbContext.Orders.AnyAsync(x => x.Code == request.Code.Trim(), cancellationToken))
            throw new InvalidOperationException($"Mã đơn hàng {request.Code} đã tồn tại.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var entity = MapNewEntity(request, creatorEmployeeId);
        dbContext.Orders.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<OrderDetailDto?> UpdateAsync(long id, SaveOrderRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        var entity = await dbContext.Orders.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;
        if (await dbContext.Orders.AnyAsync(x => x.Code == request.Code.Trim() && x.Id != id, cancellationToken))
            throw new InvalidOperationException($"Mã đơn hàng {request.Code} đã tồn tại.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        entity.Code = request.Code.Trim();
        entity.CustomerId = request.CustomerId;
        entity.DeliveryEmployeeId = request.DeliveryEmployeeId;
        entity.OrderedAt = request.OrderedAt;
        entity.ExpectedDeliveryAt = request.ExpectedDeliveryAt;
        entity.DeliveredAt = request.DeliveredAt;
        entity.DeliveryAddress = request.DeliveryAddress.Trim();
        entity.DiscountAmount = request.DiscountAmount;
        entity.TaxAmount = request.TaxAmount;
        entity.ShippingFee = request.ShippingFee;
        entity.Status = request.Status;
        entity.Note = request.Note?.Trim();
        entity.UpdatedAt = DateTime.Now;
        dbContext.OrderItems.RemoveRange(entity.Items);
        entity.Items = request.Items.Select(MapItem).ToList();
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Orders.FindAsync([id], cancellationToken);
        if (entity is null) return false;
        dbContext.Orders.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static OrderEntity MapNewEntity(SaveOrderRequest request, int creatorEmployeeId) => new()
    {
        Code = request.Code.Trim(), CustomerId = request.CustomerId,
        ManagerEmployeeId = creatorEmployeeId, DeliveryEmployeeId = request.DeliveryEmployeeId,
        OrderedAt = request.OrderedAt, ExpectedDeliveryAt = request.ExpectedDeliveryAt,
        DeliveredAt = request.DeliveredAt, DeliveryAddress = request.DeliveryAddress.Trim(),
        DiscountAmount = request.DiscountAmount, TaxAmount = request.TaxAmount,
        ShippingFee = request.ShippingFee, Status = request.Status, Note = request.Note?.Trim(),
        Items = request.Items.Select(MapItem).ToList()
    };

    private static OrderItemEntity MapItem(SaveOrderItemRequest item) => new()
    {
        ProductId = item.ProductId, Quantity = item.Quantity, UnitPrice = item.UnitPrice,
        DiscountPercent = item.DiscountPercent, Note = item.Note?.Trim()
    };

    private static void ValidateRequest(SaveOrderRequest request)
    {
        if (!ValidStatuses.Contains(request.Status)) throw new ArgumentException("Trạng thái đơn hàng không hợp lệ.");
        if (request.Items.Select(x => x.ProductId).Distinct().Count() != request.Items.Count)
            throw new ArgumentException("Một hàng hóa không được xuất hiện nhiều lần trong cùng đơn hàng.");
        var now = DateTime.Now;
        if (request.OrderedAt > now.AddMinutes(1) || request.OrderedAt < now.AddYears(-10))
            throw new ArgumentException("Ngày đặt hàng phải từ 10 năm trước đến thời điểm hiện tại.");
        if (!request.ExpectedDeliveryAt.HasValue || request.ExpectedDeliveryAt.Value <= now)
            throw new ArgumentException("Ngày và giờ giao dự kiến phải sau thời điểm hiện tại.");
        if (request.ExpectedDeliveryAt.Value <= request.OrderedAt)
            throw new ArgumentException("Ngày giao dự kiến phải sau ngày đặt hàng.");
        if (request.DeliveredAt.HasValue && request.DeliveredAt.Value < request.OrderedAt)
            throw new ArgumentException("Ngày giao thực tế không được trước ngày đặt hàng.");
        var merchandiseTotal = request.Items.Sum(x => x.Quantity * x.UnitPrice * (1 - x.DiscountPercent / 100m));
        var grandTotal = merchandiseTotal - request.DiscountAmount + request.TaxAmount + request.ShippingFee;
        if (grandTotal < 0) throw new ArgumentException("Tổng thanh toán không được âm. Vui lòng kiểm tra tiền giảm giá.");
    }
}
