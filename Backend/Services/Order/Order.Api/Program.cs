using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Sv.Order.Data;
using Sv.Order.Repository.Implement;
using Sv.Order.Repository.Interface;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("OrderDatabase")
    ?? throw new InvalidOperationException("Thiếu ConnectionStrings:OrderDatabase.");

builder.Services.AddControllers();
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
});
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddPolicy("AngularClient", policy =>
    policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();
app.UseCors("AngularClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { service = "Order.Api", status = "healthy" }));
app.Run();
