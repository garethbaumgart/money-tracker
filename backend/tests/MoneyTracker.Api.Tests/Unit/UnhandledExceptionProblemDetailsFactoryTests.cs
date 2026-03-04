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
        Assert.Equal("https://www.rfc-editor.org/rfc/rfc9110#section-15.6.1", problemDetails.Type);
        Assert.Equal("An unexpected error occurred.", problemDetails.Title);
        Assert.Equal(
            "The server encountered an unexpected error while processing the request.",
            problemDetails.Detail);
        Assert.Equal("/transactions", problemDetails.Instance);
        Assert.Equal(new[] { "code", "traceId" }, problemDetails.Extensions.Keys.OrderBy(key => key));
        Assert.True(problemDetails.Extensions.TryGetValue("code", out var code));
        Assert.True(problemDetails.Extensions.TryGetValue("traceId", out var traceId));
        Assert.Equal("internal_server_error", code);
        Assert.Equal("trace-unit-123", traceId);
    }
}
