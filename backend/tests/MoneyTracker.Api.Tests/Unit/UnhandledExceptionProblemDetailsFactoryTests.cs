using Microsoft.AspNetCore.Http;
using MoneyTracker.Api.Diagnostics;

namespace MoneyTracker.Api.Tests.Unit;

public sealed class UnhandledExceptionProblemDetailsFactoryTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Create_ReturnsExpectedProblemDetailsShape()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "trace-unit-123";
        httpContext.Request.Path = "/transactions";

        var problemDetails = UnhandledExceptionProblemDetailsFactory.Create(httpContext);

        Assert.Equal(StatusCodes.Status500InternalServerError, problemDetails.Status);
        Assert.Equal("https://httpstatuses.com/500", problemDetails.Type);
        Assert.Equal("An unexpected error occurred.", problemDetails.Title);
        Assert.Equal(
            "The server encountered an unexpected error while processing the request.",
            problemDetails.Detail);
        Assert.Equal("/transactions", problemDetails.Instance);
        Assert.Equal("internal_server_error", problemDetails.Extensions["code"]);
        Assert.Equal("trace-unit-123", problemDetails.Extensions["traceId"]);
    }
}
