using Microsoft.Extensions.Options;
using MoneyTracker.Api.Configuration;

namespace MoneyTracker.Api.Security;

internal sealed class PayloadSizeLimitMiddleware(
    RequestDelegate next,
    IOptions<SecurityOptions> securityOptions)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var maxSize = securityOptions.Value.MaxPayloadSizeBytes;

        if (context.Request.ContentLength.HasValue && context.Request.ContentLength.Value > maxSize)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                status = 413,
                title = "Payload too large.",
                detail = $"Request body exceeds the maximum allowed size of {maxSize} bytes.",
                code = "payload_too_large"
            });
            return;
        }

        await next(context);
    }
}
