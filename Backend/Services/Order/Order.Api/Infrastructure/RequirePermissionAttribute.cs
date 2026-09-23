using Microsoft.AspNetCore.Authorization;

namespace Order.Api.Infrastructure;

public sealed class RequirePermissionAttribute(string permission) : AuthorizeAttribute($"permission:{permission}")
{
    public static readonly string[] Codes =
    [
        "ORDER_VIEW", "ORDER_CREATE", "ORDER_UPDATE", "ORDER_DELETE", "ORDER_APPROVE", "ORDER_DELIVERY",
        "CUSTOMER_VIEW", "CUSTOMER_MANAGE", "PRODUCT_VIEW", "PRODUCT_MANAGE", "PERMISSION_MANAGE"
    ];
}
