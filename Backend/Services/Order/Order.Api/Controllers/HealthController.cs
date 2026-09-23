using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sv.Order.DTOs;

namespace Order.Api.Controllers;

[Route("api/health")]
public sealed class HealthController : ApiControllerBase
{
    [HttpPost, AllowAnonymous]
    public IActionResult Check(EmptyRequest request) =>
        Success(new { Service = "Order.Api", Healthy = true });
}
