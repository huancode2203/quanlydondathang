using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

// Opt-in integration tests: creates and drops ONLY a uniquely named scratch database.
// Production data is never copied or changed. Run with --database on a local SQL dev instance.
internal static class DatabaseChecks
{
    public static async Task RunAsync()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.."));
        var apiPath = Path.Combine(root, "Backend/Services/Order/Order.Api");
        var config = new ConfigurationBuilder().AddJsonFile(Path.Combine(apiPath, "appsettings.json")).Build();
        var connection = new SqlConnectionStringBuilder(config.GetConnectionString("OrderDatabase"));
        var database = "OrderTest_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = "master";
        await using var admin = new SqlConnection(connection.ConnectionString);
        await admin.OpenAsync();
        try
        {
            var scripts = new[] { "QuanLyDonDatHangDB.sql", "002_AddProductStock.sql", "003_EnsureAdminFullPermissions.sql",
                "004_AddAccountPermissions.sql", "005_AddOrderStockWorkflow.sql", "006_AddMultipleAccountRoles.sql",
                "007_NormalizeDomains.sql", "008_OrderDiscountsAndTaxes.sql" };
            foreach (var name in scripts)
            {
                var sql = (await File.ReadAllTextAsync(Path.Combine(root, "Database", name)))
                    .Replace("QuanLyDonDatHangDB", database).Replace("$(ApplyChanges)", "1");
                var batches = Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                for (var index = 0; index < batches.Length; index++)
                {
                    var batch = batches[index];
                    if (string.IsNullOrWhiteSpace(batch)) continue;
                    await using var command = new SqlCommand(batch, admin) { CommandTimeout = 60 };
                    try { await command.ExecuteNonQueryAsync(); }
                    catch (Exception exception) { throw new InvalidOperationException($"Database initialization failed at {name}, batch {index + 1}.", exception); }
                }
            }
            connection.InitialCatalog = database;
            using var factory = new DatabaseFactory(apiPath, connection.ConnectionString);
            using var client = factory.CreateClient();
            var count = 0;
            async Task<JsonElement> Post(string path, object payload, int status = 200)
            {
                using var response = await client.PostAsJsonAsync("/api/" + path, payload);
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                if ((int)response.StatusCode != status || json.GetProperty("status").GetInt32() != status)
                    throw new Exception($"{path}: expected {status}, received {response.StatusCode}: {json}");
                count++;
                return json.GetProperty("value");
            }
            var login = await Post("auth/login", new { username = "admin", password = "123456" });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("token").GetString());
            var customer = await Post("customers/create", new { code = "TEST-C", name = "Test Customer" }, 201);
            var customerId = customer.GetProperty("id").GetInt32();
            var product = await Post("products/create", new { code = "TEST-P", name = "Test Product", unit = "Cai", price = 100, stockQuantity = 10 }, 201);
            var productId = product.GetProperty("id").GetInt32();
            await Post("products/create", new { code = "TEST-P", name = "Duplicate", unit = "Cai", price = 100, stockQuantity = 10 }, 409);
            var now = DateTime.Now;
            object Order(string code, decimal quantity, string status = "CHO_XAC_NHAN", long? id = null) => new
            {
                // Dictionary below omits id for create, preserving strict JSON contract.
                code, customerId, deliveryEmployeeId = 3, orderedAt = now.AddMinutes(-10),
                expectedDeliveryAt = now.AddDays(1), deliveryAddress = "Test Address", status,
                discountAmount = 20, taxPercent = 10, shippingFee = 2,
                items = new[] { new { productId, quantity, unitPrice = 100, discountPercent = 10,
                    discountAmount = 5, taxPercent = 5 } }
            };
            object Update(long id, string code, decimal quantity, string status)
            {
                var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(Order(code, quantity, status)))!;
                values["id"] = JsonSerializer.SerializeToElement(id);
                return values;
            }
            async Task Stock(decimal physical, decimal available)
            {
                var rows = await Post("products/search", new { keyword = "TEST-P" });
                var row = rows.EnumerateArray().Single();
                if (row.GetProperty("stockQuantity").GetDecimal() != physical || row.GetProperty("availableQuantity").GetDecimal() != available)
                    throw new Exception($"Incorrect stock: {row}");
            }
            var order = await Post("orders/create", Order("TEST-O", 2), 201);
            var orderId = order.GetProperty("id").GetInt64();
            if (order.GetProperty("merchandiseTotal").GetDecimal() != 175 ||
                order.GetProperty("taxAmount").GetDecimal() != 24.25m ||
                order.GetProperty("grandTotal").GetDecimal() != 181.25m)
                throw new Exception("Percentage/fixed discounts or stacked taxes were calculated incorrectly");
            var orderDetail = await Post("orders/detail", new { id = orderId });
            var line = orderDetail.GetProperty("items")[0];
            if (line.GetProperty("discountAmount").GetDecimal() != 5 ||
                line.GetProperty("taxPercent").GetDecimal() != 5 || line.GetProperty("taxAmount").GetDecimal() != 8.75m)
                throw new Exception("Line discount/tax rates were not persisted and returned");
            await Stock(10, 8);
            await Post("orders/update", Update(orderId, "TEST-O", 12, "CHO_XAC_NHAN"));
            await Stock(10, -2);
            foreach (var status in new[] { "DA_XAC_NHAN", "DANG_CHUAN_BI", "CHO_GIAO_HANG", "DANG_GIAO" })
                await Post("orders/update", Update(orderId, "TEST-O", 12, status));
            await Post("orders/update", Update(orderId, "TEST-O", 12, "DA_GIAO"), 409);
            await Stock(10, -2);
            await Post("orders/update", Update(orderId, "TEST-O", 3, "DA_GIAO"));
            await Stock(7, 7);
            await Post("orders/update", Update(orderId, "TEST-O", 3, "DA_GIAO"), 409);
            await Stock(7, 7);
            await Post("orders/delete", new { id = orderId }, 409);
            var canceled = await Post("orders/create", Order("TEST-CANCEL", 2), 201);
            await Stock(7, 5);
            await Post("orders/update", Update(canceled.GetProperty("id").GetInt64(), "TEST-CANCEL", 2, "DA_HUY"));
            await Stock(7, 7);
            var deleted = await Post("orders/create", Order("TEST-DELETE", 1), 201);
            await Post("orders/delete", new { id = deleted.GetProperty("id").GetInt64() });
            await Stock(7, 7);
            await Post("permissions/search", new { });
            Console.WriteLine($"PASS: fresh schema + migrations and {count} SQL-backed HTTP checks (CRUD, totals, stock, terminal states).");
        }
        finally
        {
            // Exact name generated above, never the configured application database.
            if (!Regex.IsMatch(database, "^OrderTest_[a-f0-9]{32}$")) throw new InvalidOperationException("Unsafe test database name");
            SqlConnection.ClearAllPools();
            await admin.ChangeDatabaseAsync("master");
            await using var cleanup = new SqlCommand($"IF DB_ID(N'{database}') IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; END", admin);
            await cleanup.ExecuteNonQueryAsync();
            Console.WriteLine("Scratch database removed; application database unchanged.");
        }
    }
}

internal sealed class DatabaseFactory(string apiPath, string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(apiPath);
        builder.UseSetting("ConnectionStrings:OrderDatabase", connectionString);
    }
}
