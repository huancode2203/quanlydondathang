using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Sv.Order.Exceptions;

namespace Order.Api.Infrastructure;

// HTTP concerns stay here; repositories only report domain failures.
public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            context.Abort();
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var (status, message) = Describe(exception);
            if (status >= 500)
                logger.LogError(exception, "Request failed. TraceId {TraceId}, {Method} {Path}",
                    context.TraceIdentifier, context.Request.Method, context.Request.Path);
            else
                logger.LogWarning("Request rejected. TraceId {TraceId}, status {Status}, exception {ExceptionType}",
                    context.TraceIdentifier, status, exception.GetType().Name);

            context.Response.Clear();
            await ApiError.WriteAsync(context, status, message);
        }
    }

    private static (int Status, string? Message) Describe(Exception exception) => exception switch
    {
        DomainValidationException known => (400, known.Message),
        DomainConflictException known => (409, known.Message),
        DbUpdateConcurrencyException => (409, null),
        DbUpdateException { InnerException: SqlException sql } => DescribeSql(sql),
        SqlException sql => DescribeSql(sql),
        BadHttpRequestException bad => (bad.StatusCode, null),
        _ => (500, null)
    };

    private static (int Status, string? Message) DescribeSql(SqlException exception) => exception.Number switch
    {
        2601 or 2627 => (409, "Mã dữ liệu đã tồn tại. Vui lòng sử dụng mã khác."),
        547 => (409, "Dữ liệu liên quan không còn hợp lệ hoặc đang được sử dụng. Vui lòng tải lại."),
        1205 or 51010 => (409, "Dữ liệu đang được xử lý đồng thời. Vui lòng thử lại."),
        -2 or 53 or 10060 or 10061 or 4060 => (503, null),
        _ => (500, null)
    };
}
