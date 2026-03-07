using Microsoft.AspNetCore.Http;
using MoneyTracker.Api.Security;

namespace MoneyTracker.Api.Tests.Unit.Security;

public sealed class SecurityHeadersMiddlewareTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task InvokeAsync_SetsHstsHeader()
    {
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal("max-age=31536000; includeSubDomains", context.Response.Headers["Strict-Transport-Security"].ToString());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InvokeAsync_SetsXContentTypeOptionsHeader()
    {
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InvokeAsync_SetsXFrameOptionsHeader()
    {
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"].ToString());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InvokeAsync_SetsCspHeader()
    {
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal("default-src 'none'", context.Response.Headers["Content-Security-Policy"].ToString());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InvokeAsync_SetsReferrerPolicyHeader()
    {
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal("no-referrer", context.Response.Headers["Referrer-Policy"].ToString());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InvokeAsync_SetsCacheControlHeader()
    {
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal("no-store", context.Response.Headers["Cache-Control"].ToString());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InvokeAsync_AllRequiredHeadersPresent()
    {
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.True(context.Response.Headers.ContainsKey("Strict-Transport-Security"));
        Assert.True(context.Response.Headers.ContainsKey("X-Content-Type-Options"));
        Assert.True(context.Response.Headers.ContainsKey("X-Frame-Options"));
        Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        Assert.True(context.Response.Headers.ContainsKey("Referrer-Policy"));
        Assert.True(context.Response.Headers.ContainsKey("Cache-Control"));
    }
}
