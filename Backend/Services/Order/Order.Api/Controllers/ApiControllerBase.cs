using Microsoft.AspNetCore.Mvc;
using Order.Api.Infrastructure;

namespace Order.Api.Controllers;

[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected ObjectResult Success<T>(T value, string message = "Thành công.", int status = 200) =>
        StatusCode(status, new ApiResponse<T>(status, value, message));

    protected ObjectResult Missing(string message) =>
        NotFound(ApiError.Create(HttpContext, 404, message));

    protected ObjectResult Forbidden(string message) =>
        StatusCode(StatusCodes.Status403Forbidden, ApiError.Create(HttpContext, 403, message));
}
