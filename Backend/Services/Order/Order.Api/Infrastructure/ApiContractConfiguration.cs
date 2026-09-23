using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Order.Api.Infrastructure;

public static class ApiContractConfiguration
{
    public static IServiceCollection AddApiContract(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
            options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
            options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
            options.JsonSerializerOptions.AllowDuplicateProperties = false;
            options.JsonSerializerOptions.MaxDepth = 16;
        });
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressMapClientErrors = true;
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState.Where(item => item.Value?.Errors.Count > 0)
                    .ToDictionary(item => NormalizeKey(item.Key), item => item.Value!.Errors
                        .Select(error => error.Exception is not null || item.Key.StartsWith('$')
                            ? "JSON không hợp lệ: kiểm tra tên trường, kiểu dữ liệu và các trường bắt buộc."
                            : string.IsNullOrWhiteSpace(error.ErrorMessage)
                                ? "Giá trị không hợp lệ."
                                : error.ErrorMessage)
                        .Distinct().ToArray());
                return new BadRequestObjectResult(ApiError.Create(context.HttpContext, 400, errors: errors));
            };
        });
        return services;
    }

    private static string NormalizeKey(string key) => string.IsNullOrEmpty(key) ? "body" :
        string.Join('.', key.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
