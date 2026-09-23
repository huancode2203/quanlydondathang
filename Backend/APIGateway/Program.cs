var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddCors(options => options.AddPolicy("AngularClient", policy =>
    policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
        .AllowAnyHeader()
        .WithMethods("POST")));

var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(context => WriteErrorAsync(context, 500,
    "Không thể xử lý yêu cầu. Vui lòng thử lại hoặc cung cấp mã tra cứu lỗi.")));
app.UseStatusCodePages(async statusContext =>
{
    var context = statusContext.HttpContext;
    var message = context.Response.StatusCode switch
    {
        404 => "Đường dẫn không tồn tại.",
        405 => "API chỉ chấp nhận phương thức POST.",
        502 or 503 or 504 => "Dịch vụ đang không sẵn sàng. Vui lòng thử lại sau.",
        _ => "Yêu cầu không thể được xử lý."
    };
    await WriteErrorAsync(context, context.Response.StatusCode, message);
});
app.UseCors("AngularClient");
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api") && !HttpMethods.IsPost(context.Request.Method))
    {
        context.Response.Headers.Allow = "POST";
        await WriteErrorAsync(context, 405, "API chỉ chấp nhận phương thức POST.");
        return;
    }
    await next();
});

// IIS serves the compiled Angular app through the same origin as the API gateway.
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapReverseProxy();
app.MapFallback(async context =>
{
    var indexFile = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "index.html");
    if (!context.Request.Path.StartsWithSegments("/api") &&
        (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)) &&
        !Path.HasExtension(context.Request.Path) && File.Exists(indexFile))
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.Headers.CacheControl = "no-cache";
        await context.Response.SendFileAsync(indexFile);
        return;
    }
    await WriteErrorAsync(context, 404, "Đường dẫn không tồn tại.");
});
app.Run();

static Task WriteErrorAsync(HttpContext context, int status, string message)
{
    context.Response.StatusCode = status;
    return context.Response.WriteAsJsonAsync(new
    {
        status,
        value = (object?)null,
        message,
        traceId = context.TraceIdentifier
    });
}
