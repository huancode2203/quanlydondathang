using Dapper;
using Microsoft.EntityFrameworkCore;
using Sv.Order.Data;
using Sv.Order.DTOs;
using Sv.Order.Entities;
using Sv.Order.Repository.Interface;

namespace Sv.Order.Repository.Implement;

public sealed class MasterDataRepository(OrderDbContext dbContext) : IMasterDataRepository
{
    public async Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(string? keyword, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT KhachHangID AS Id, MaKhachHang AS Code, TenKhachHang AS Name,
                   SoDienThoai AS Phone, Email, DiaChi AS Address, MaSoThue AS TaxCode, GhiChu AS Note
            FROM tbl_KhachHang
            WHERE IsDeleted = 0 AND (@Keyword IS NULL OR MaKhachHang LIKE @Pattern OR TenKhachHang LIKE @Pattern OR SoDienThoai LIKE @Pattern)
            ORDER BY TenKhachHang;
            """;
        var key = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
        var connection = dbContext.Database.GetDbConnection();
        return (await connection.QueryAsync<CustomerDto>(new CommandDefinition(sql,
            new { Keyword = key, Pattern = $"%{key}%" }, cancellationToken: cancellationToken))).AsList();
    }

    public async Task<CustomerDto> CreateCustomerAsync(SaveCustomerRequest request, CancellationToken cancellationToken)
    {
        await EnsureCustomerCodeAsync(request.Code, null, cancellationToken);
        var entity = new CustomerEntity
        {
            Code = request.Code.Trim(), Name = request.Name.Trim(), Phone = Clean(request.Phone),
            Email = Clean(request.Email), Address = Clean(request.Address), TaxCode = Clean(request.TaxCode), Note = Clean(request.Note)
        };
        dbContext.Customers.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<CustomerDto?> UpdateCustomerAsync(int id, SaveCustomerRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Customers.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (entity is null) return null;
        await EnsureCustomerCodeAsync(request.Code, id, cancellationToken);
        entity.Code = request.Code.Trim(); entity.Name = request.Name.Trim(); entity.Phone = Clean(request.Phone);
        entity.Email = Clean(request.Email); entity.Address = Clean(request.Address); entity.TaxCode = Clean(request.TaxCode);
        entity.Note = Clean(request.Note); entity.UpdatedAt = DateTime.Now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> DeleteCustomerAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Customers.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (entity is null) return false;
        entity.IsDeleted = true; entity.Status = "INACTIVE"; entity.UpdatedAt = DateTime.Now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(string? keyword, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT HangHoaID AS Id, MaHang AS Code, TenHang AS Name, DonViTinh AS Unit,
                   GiaBan AS Price, SoLuongTon AS StockQuantity, MoTa AS Description
            FROM tbl_HangHoa
            WHERE IsDeleted = 0 AND (@Keyword IS NULL OR MaHang LIKE @Pattern OR TenHang LIKE @Pattern)
            ORDER BY TenHang;
            """;
        var key = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
        var connection = dbContext.Database.GetDbConnection();
        return (await connection.QueryAsync<ProductDto>(new CommandDefinition(sql,
            new { Keyword = key, Pattern = $"%{key}%" }, cancellationToken: cancellationToken))).AsList();
    }

    public async Task<ProductDto> CreateProductAsync(SaveProductRequest request, CancellationToken cancellationToken)
    {
        await EnsureProductCodeAsync(request.Code, null, cancellationToken);
        var entity = new ProductEntity
        {
            Code = request.Code.Trim(), Name = request.Name.Trim(), Unit = request.Unit.Trim(),
            Price = request.Price, StockQuantity = request.StockQuantity, Description = Clean(request.Description)
        };
        dbContext.Products.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<ProductDto?> UpdateProductAsync(int id, SaveProductRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Products.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (entity is null) return null;
        await EnsureProductCodeAsync(request.Code, id, cancellationToken);
        entity.Code = request.Code.Trim(); entity.Name = request.Name.Trim(); entity.Unit = request.Unit.Trim();
        entity.Price = request.Price; entity.StockQuantity = request.StockQuantity;
        entity.Description = Clean(request.Description); entity.UpdatedAt = DateTime.Now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> DeleteProductAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Products.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (entity is null) return false;
        entity.IsDeleted = true; entity.Status = "INACTIVE"; entity.UpdatedAt = DateTime.Now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureCustomerCodeAsync(string code, int? exceptId, CancellationToken token)
    {
        if (await dbContext.Customers.AnyAsync(x => x.Code == code.Trim() && x.Id != exceptId, token))
            throw new InvalidOperationException($"Mã khách hàng {code} đã tồn tại.");
    }

    private async Task EnsureProductCodeAsync(string code, int? exceptId, CancellationToken token)
    {
        if (await dbContext.Products.AnyAsync(x => x.Code == code.Trim() && x.Id != exceptId, token))
            throw new InvalidOperationException($"Mã hàng hóa {code} đã tồn tại.");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static CustomerDto Map(CustomerEntity x) => new() { Id = x.Id, Code = x.Code, Name = x.Name, Phone = x.Phone, Email = x.Email, Address = x.Address, TaxCode = x.TaxCode, Note = x.Note };
    private static ProductDto Map(ProductEntity x) => new() { Id = x.Id, Code = x.Code, Name = x.Name, Unit = x.Unit, Price = x.Price, StockQuantity = x.StockQuantity, Description = x.Description };
}
