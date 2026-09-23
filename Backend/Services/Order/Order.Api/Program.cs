using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Order.Api.Infrastructure;
using Sv.Order.Data;
using Sv.Order.Repository.Implement;
using Sv.Order.Repository.Interface;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("OrderDatabase")
    ?? throw new InvalidOperationException("Thiếu ConnectionStrings:OrderDatabase.");

builder.Services.AddApiContract();
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = ApiRequestMiddleware.MaxBodyBytes * 2);
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.CommandTimeout(30)));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IMasterDataRepository, MasterDataRepository>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            if (!int.TryParse(context.Principal?.FindFirst("employee_id")?.Value, out var employeeId) || employeeId <= 0)
                context.Fail("Invalid employee claim.");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.Headers.WWWAuthenticate = "Bearer";
            return ApiError.WriteAsync(context.HttpContext, 401);
        },
        OnForbidden = context => ApiError.WriteAsync(context.HttpContext, 403)
    };
});
builder.Services.AddAuthorization(options =>
{
    foreach (var code in RequirePermissionAttribute.Codes)
        options.AddPolicy($"permission:{code}", policy =>
        {
            policy.RequireAuthenticatedUser().RequireClaim("permission", code);
            // An action permission is never usable without its module's view permission.
            var feature = code.Split('_')[0];
            if (feature is "ORDER" or "CUSTOMER" or "PRODUCT")
                policy.RequireClaim("permission", $"{feature}_VIEW");
        });
});
builder.Services.AddCors(options => options.AddPolicy("AngularClient", policy =>
    policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
        .AllowAnyHeader()
        .WithMethods("POST")));

var app = builder.Build();
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseStatusCodePages(context => ApiError.WriteAsync(context.HttpContext, context.HttpContext.Response.StatusCode));
app.UseRouting();
app.UseCors("AngularClient");
app.UseMiddleware<ApiRequestMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

// Exposes the entry point to isolated HTTP contract tests; it adds no endpoint.
public partial class Program;
