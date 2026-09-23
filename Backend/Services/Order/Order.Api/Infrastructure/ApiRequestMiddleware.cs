using Microsoft.AspNetCore.Http.Features;

namespace Order.Api.Infrastructure;

public sealed class ApiRequestMiddleware(RequestDelegate next)
{
    public const long MaxBodyBytes = 1024 * 1024;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        // CORS preflight is handled by UseCors before reaching this middleware.
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            context.Response.Headers.Allow = "POST";
            await ApiError.WriteAsync(context, 405);
            return;
        }
        if (context.Request.QueryString.HasValue)
        {
            await ApiError.WriteAsync(context, 400, "Các tham số phải được gửi trong JSON body, không đặt trên URL.");
            return;
        }
        if (context.Request.ContentLength > MaxBodyBytes)
        {
            await ApiError.WriteAsync(context, 413);
            return;
        }

        var bodyLimit = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (bodyLimit is { IsReadOnly: false }) bodyLimit.MaxRequestBodySize = MaxBodyBytes;
        await next(context);
    }
}
