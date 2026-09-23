using System.Text.Json.Serialization;

namespace Order.Api.Infrastructure;

public sealed record ApiResponse<T>(
    int Status,
    T? Value,
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<string, string[]>? Errors = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? TraceId = null);

public static class ApiError
{
    public static ApiResponse<object> Create(HttpContext context, int status, string? message = null,
        IReadOnlyDictionary<string, string[]>? errors = null) =>
        new(status, null, message ?? Message(status), errors, context.TraceIdentifier);

    public static Task WriteAsync(HttpContext context, int status, string? message = null,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(Create(context, status, message, errors), context.RequestAborted);
    }

    private static string Message(int status) => status switch
    {
        400 => "Dữ liệu yêu cầu không hợp lệ. Vui lòng kiểm tra lại các trường nhập.",
        401 => "Vui lòng đăng nhập lại để tiếp tục.",
        403 => "Tài khoản không có quyền thực hiện chức năng này.",
        404 => "Không tìm thấy chức năng hoặc dữ liệu được yêu cầu.",
        405 => "API chỉ chấp nhận phương thức POST.",
        409 => "Dữ liệu đã thay đổi hoặc bị trùng. Vui lòng tải lại và thử lại.",
        413 => "Nội dung yêu cầu vượt quá giới hạn 1 MB.",
        415 => "Yêu cầu phải có Content-Type: application/json.",
        429 => "Có quá nhiều yêu cầu. Vui lòng thử lại sau.",
        503 => "Dịch vụ tạm thời chưa sẵn sàng. Vui lòng thử lại sau.",
        _ => "Đã xảy ra lỗi hệ thống. Vui lòng thử lại hoặc cung cấp mã tra cứu cho người quản trị."
    };
}
