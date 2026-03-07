using Microsoft.AspNetCore.Http;
using MoneyTracker.Modules.SharedKernel.Presentation;

namespace MoneyTracker.Modules.Households.Presentation;

internal static class HouseholdEndpointHelpers
{
    internal static Task<(bool Success, AuthenticatedUser? AuthenticatedUser, IResult? Problem)> ResolveAuthenticatedUser(
        HttpContext httpContext)
    {
        return EndpointHelpers.ResolveAuthenticatedUser(httpContext);
    }

    internal static IResult BuildProblemResult(int statusCode, string title, string detail, string code, string instance)
    {
        return EndpointHelpers.BuildProblemResult(statusCode, title, detail, code, instance);
    }

    internal static Task WriteProblemAsync(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail,
        string? code,
        string fallbackCode)
    {
        return EndpointHelpers.WriteProblemAsync(httpContext, statusCode, title, detail, code, fallbackCode);
    }
}
