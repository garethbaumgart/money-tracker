using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using MoneyTracker.Api.Diagnostics;
using MoneyTracker.Api.Observability;

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
        var expectedTraceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        var problemDetails = UnhandledExceptionProblemDetailsFactory.Create(httpContext);

        Assert.Equal(StatusCodes.Status500InternalServerError, problemDetails.Status);
        Assert.Equal("https://www.rfc-editor.org/rfc/rfc9110#section-15.6.1", problemDetails.Type);
        Assert.Equal("An unexpected error occurred.", problemDetails.Title);
        Assert.Equal(
            "The server encountered an unexpected error while processing the request.",
            problemDetails.Detail);
        Assert.Equal("/transactions", problemDetails.Instance);
        Assert.Equal(new[] { "code", "correlationId", "traceId" }, problemDetails.Extensions.Keys.OrderBy(key => key));
        Assert.True(problemDetails.Extensions.TryGetValue("code", out var code));
        Assert.True(problemDetails.Extensions.TryGetValue("traceId", out var traceId));
        Assert.True(problemDetails.Extensions.TryGetValue("correlationId", out var correlationId));
        Assert.Equal("internal_server_error", code);
        Assert.Equal(expectedTraceId, traceId);
        Assert.Equal(httpContext.TraceIdentifier, correlationId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Create_UsesCurrentActivityTraceId_WhenPresent()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "trace-unit-456";
        httpContext.Items[CorrelationHeaders.CorrelationIdItemKey] = "corr-456";
        using var activity = new Activity("unit-test");
        activity.Start();

        var problemDetails = UnhandledExceptionProblemDetailsFactory.Create(httpContext);

        Assert.True(problemDetails.Extensions.TryGetValue("traceId", out var traceId));
        Assert.Equal(activity.TraceId.ToString(), traceId);
        Assert.Equal("corr-456", problemDetails.Extensions["correlationId"]);
    }
}
