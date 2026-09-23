using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Sv.Order.DTOs;
using Sv.Order.Exceptions;
using Sv.Order.Repository.Interface;

// Uses the real HTTP pipeline/controllers with an isolated order repository.
// No SQL connection, account change or production test endpoint is needed.
internal static class ContractChecks
{
public static async Task Main(string[] args)
{
if (args.Contains("--database")) { await DatabaseChecks.RunAsync(); return; }
using var factory = new ContractFactory();
using var client = factory.CreateClient();
var passed = 0;
await Check("health", "/api/health", "{}", 200);
await Check("method GET", "/api/health", "{}", 405, HttpMethod.Get);
await Check("method PUT", "/api/health", "{}", 405, HttpMethod.Put);
await Check("method DELETE", "/api/health", "{}", 405, HttpMethod.Delete);
await Check("unknown route", "/api/not-found", "{}", 404);
await Check("query not allowed", "/api/health?q=1", "{}", 400);
await Check("malformed JSON", "/api/health", "{", 400);
await Check("unknown property", "/api/health", "{\"extra\":1}", 400);
await Check("null body", "/api/health", "null", 400);
await Check("empty body", "/api/health", "", 400);
await Check("media type", "/api/health", "{}", 415, mediaType: "text/plain");
await Check("body limit", "/api/health", new string(' ', 1024 * 1024 + 1), 413);
await Check("anonymous search", "/api/orders/search", "{}", 401);

SetToken("ORDER_UPDATE");
await Check("action without view", "/api/orders/update", "{}", 403);
SetToken("ORDER_VIEW");
await Check("read-only cannot delete", "/api/orders/delete", "{\"id\":1}", 403);
SetToken("ORDER_VIEW", "ORDER_UPDATE", "ORDER_CREATE", "ORDER_DELETE", "PERMISSION_MANAGE");
await Check("search defaults", "/api/orders/search", "{}", 200);
await Check("invalid page", "/api/orders/search", "{\"page\":0}", 400);
await Check("oversized page", "/api/orders/search", "{\"pageSize\":101}", 400);
await Check("number as string", "/api/orders/search", "{\"page\":\"1\"}", 400);
await Check("duplicate property", "/api/orders/search", "{\"page\":1,\"page\":2}", 400);
await Check("wrong casing", "/api/orders/search", "{\"Page\":1}", 400);
await Check("invalid enum", "/api/orders/search", "{\"status\":\"UNKNOWN\"}", 400);
await Check("inverted total", "/api/orders/search", "{\"minTotal\":2,\"maxTotal\":1}", 400);
await Check("decimal precision", "/api/orders/search", "{\"minTotal\":0.001}", 400);
await Check("date overflow", "/api/orders/search", "{\"toDate\":\"9999-12-31\"}", 400);
await Check("inverted dates", "/api/orders/search", "{\"fromDate\":\"2026-02-02\",\"toDate\":\"2026-01-01\"}", 400);
await Check("invalid id", "/api/orders/detail", "{\"id\":0}", 400);
await Check("missing record", "/api/orders/detail", "{\"id\":999}", 404);
await Check("domain conflict", "/api/orders/delete", "{\"id\":409}", 409);
await Check("domain validation", "/api/orders/delete", "{\"id\":400}", 400);
await Check("unexpected exception", "/api/orders/delete", "{\"id\":500}", 500);
const string updateTemplate = "\"code\":\"CURRENT\",\"customerId\":1,\"deliveryEmployeeId\":3,\"orderedAt\":\"2026-09-23T08:00:00\",\"expectedDeliveryAt\":\"2026-09-24T08:00:00\",\"deliveryAddress\":\"Test Address\",\"discountAmount\":0,\"taxPercent\":0,\"shippingFee\":0,\"status\":\"{0}\",\"items\":[{{\"productId\":1,\"quantity\":1,\"unitPrice\":100,\"discountPercent\":0,\"discountAmount\":0,\"taxPercent\":0}}]";
SetToken("ORDER_VIEW", "ORDER_UPDATE");
await Check("UPDATE cannot approve without granular right", "/api/orders/update", "{\"id\":1," + string.Format(updateTemplate, "DA_XAC_NHAN") + "}", 403);
SetToken("ORDER_VIEW", "ORDER_UPDATE", "ORDER_APPROVE");
await Check("approver can advance status", "/api/orders/update", "{\"id\":1," + string.Format(updateTemplate, "DA_XAC_NHAN") + "}", 200);
SetToken("ORDER_VIEW", "ORDER_DELIVERY");
await Check("delivery role can mark delivered", "/api/orders/update", "{\"id\":2," + string.Format(updateTemplate, "DA_GIAO") + "}", 200);
await Check("delivery role cannot edit order items", "/api/orders/update", "{\"id\":2," + string.Format(updateTemplate.Replace("\"customerId\":1", "\"customerId\":2"), "DA_GIAO") + "}", 403);
SetToken("ORDER_VIEW", "ORDER_CREATE", "ORDER_UPDATE", "ORDER_APPROVE", "ORDER_DELIVERY", "ORDER_DELETE", "PERMISSION_MANAGE");
await Check("role update requires explicit list", "/api/permissions/roles/update", "{\"id\":1}", 400);
await Check("account update requires explicit mode", "/api/permissions/accounts/update", "{\"id\":1,\"roleIds\":[1],\"permissionIds\":[]}", 400);
const string validOrder = """
{"code":"TEST","customerId":1,"orderedAt":"2026-09-01T08:00:00","expectedDeliveryAt":"2026-09-30T08:00:00","deliveryAddress":"Test","taxPercent":0,"items":[{"productId":1,"quantity":1,"unitPrice":100,"discountAmount":0,"taxPercent":0}]}
""";
await Check("create success envelope", "/api/orders/create", validOrder, 201);
await Check("null line", "/api/orders/create", validOrder.Replace(
    "{\"productId\":1,\"quantity\":1,\"unitPrice\":100,\"discountAmount\":0,\"taxPercent\":0}", "null"), 400);
await Check("missing item price", "/api/orders/create", validOrder.Replace(",\"unitPrice\":100", ""), 400);
Console.WriteLine($"PASS: {passed} isolated HTTP contract checks; no database mutations.");

async Task Check(string name, string path, string body, int expected, HttpMethod? method = null, string mediaType = "application/json")
{
    using var request = new HttpRequestMessage(method ?? HttpMethod.Post, path)
    { Content = new StringContent(body, Encoding.UTF8, mediaType) };
    using var response = await client.SendAsync(request);
    var text = await response.Content.ReadAsStringAsync();
    using var json = JsonDocument.Parse(text);
    var root = json.RootElement;
    if ((int)response.StatusCode != expected || root.GetProperty("status").GetInt32() != expected ||
        !root.TryGetProperty("value", out _) || string.IsNullOrWhiteSpace(root.GetProperty("message").GetString()))
        throw new Exception($"FAIL {name}: expected {expected}; received {(int)response.StatusCode}: {text}");
    if (expected >= 400 && (!root.TryGetProperty("traceId", out _) || root.GetProperty("value").ValueKind != JsonValueKind.Null))
        throw new Exception($"FAIL {name}: missing error metadata");
    if (text.Contains("secret-internal-details")) throw new Exception("Internal exception leaked to client");
    passed++;
}

void SetToken(params string[] permissions)
{
    var config = factory.Services.GetRequiredService<IConfiguration>();
    var claims = permissions.Select(p => new Claim("permission", p)).Append(new Claim("employee_id", "1"));
    var token = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"], claims,
        expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256));
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
}
}
}

sealed class ContractFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../../Services/Order/Order.Api")));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IOrderRepository>();
            services.AddSingleton<IOrderRepository, FakeOrderRepository>();
        });
    }
}

sealed class FakeOrderRepository : IOrderRepository
{
    public Task<PagedResult<OrderListItemDto>> SearchAsync(OrderSearchRequest request, CancellationToken token) =>
        Task.FromResult(new PagedResult<OrderListItemDto>([], 0, request.Page, request.PageSize));
    public Task<OrderDetailDto?> GetByIdAsync(long id, CancellationToken token) => Task.FromResult<OrderDetailDto?>(id is 1 or 2 ? new OrderDetailDto
    {
        Id = id, Code = "CURRENT", CustomerId = 1, OrderedAt = new DateTime(2026, 9, 23, 8, 0, 0),
        ExpectedDeliveryAt = new DateTime(2026, 9, 24, 8, 0, 0), DeliveryEmployeeId = 3,
        DeliveryAddress = "Test Address", Status = id == 1 ? "CHO_XAC_NHAN" : "DANG_GIAO",
        Items = [new OrderItemDto { ProductId = 1, Quantity = 1, UnitPrice = 100, DiscountPercent = 0 }]
    } : null);
    public Task<OrderLookupsDto> GetLookupsAsync(CancellationToken token) => Task.FromResult(new OrderLookupsDto([], [], []));
    public Task<OrderDetailDto> CreateAsync(SaveOrderRequest request, int employeeId, CancellationToken token) => Task.FromResult(new OrderDetailDto { Id = 1 });
    public Task<OrderDetailDto?> UpdateAsync(long id, SaveOrderRequest request, CancellationToken token) => Task.FromResult<OrderDetailDto?>(new OrderDetailDto { Id = id, Status = request.Status });
    public Task<bool> DeleteAsync(long id, CancellationToken token) => id switch
    {
        400 => throw new DomainValidationException("Invalid domain value"),
        409 => throw new DomainConflictException("Conflicting data"),
        500 => throw new InvalidOperationException("secret-internal-details"),
        _ => Task.FromResult(false)
    };
}
