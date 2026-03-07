using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MoneyTracker.Api.Configuration;
using MoneyTracker.Api.Security;

namespace MoneyTracker.Api.Tests.Unit.Security;

public sealed class PayloadSizeLimitTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task InvokeAsync_AllowsNormalSizedPayload()
    {
        var options = Options.Create(new SecurityOptions { MaxPayloadSizeBytes = 1_048_576 });
        var nextCalled = false;
        var middleware = new PayloadSizeLimitMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, options);

        var context = new DefaultHttpContext();
        context.Request.ContentLength = 1024; // 1KB

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.NotEqual(StatusCodes.Status413PayloadTooLarge, context.Response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InvokeAsync_RejectsOversizedPayload()
    {
        var options = Options.Create(new SecurityOptions { MaxPayloadSizeBytes = 1_048_576 });
        var nextCalled = false;
        var middleware = new PayloadSizeLimitMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, options);

        var context = new DefaultHttpContext();
        context.Request.ContentLength = 2_000_000; // 2MB, exceeds 1MB limit
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, context.Response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InvokeAsync_AllowsRequestWithNoContentLength()
    {
        var options = Options.Create(new SecurityOptions { MaxPayloadSizeBytes = 1_048_576 });
        var nextCalled = false;
        var middleware = new PayloadSizeLimitMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, options);

        var context = new DefaultHttpContext();
        // No ContentLength set

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InvokeAsync_AllowsExactlyAtLimit()
    {
        var options = Options.Create(new SecurityOptions { MaxPayloadSizeBytes = 1_048_576 });
        var nextCalled = false;
        var middleware = new PayloadSizeLimitMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, options);

        var context = new DefaultHttpContext();
        context.Request.ContentLength = 1_048_576; // Exactly at limit

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }
}
