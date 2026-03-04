using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MoneyTracker.Api.Diagnostics;

internal static class UnhandledExceptionProblemDetailsFactory
{
    public static ProblemDetails Create(HttpContext httpContext)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://www.rfc-editor.org/rfc/rfc9110#section-15.6.1",
            Title = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "The server encountered an unexpected error while processing the request.",
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["code"] = ApiErrorCodes.InternalServerError;
        problemDetails.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        return problemDetails;
    }
}
