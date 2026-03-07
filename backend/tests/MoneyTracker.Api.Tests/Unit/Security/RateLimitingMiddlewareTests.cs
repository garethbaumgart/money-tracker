using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MoneyTracker.Api.Configuration;
using MoneyTracker.Api.Security;

namespace MoneyTracker.Api.Tests.Unit.Security;

public sealed class RateLimitingMiddlewareTests
{
    private static RateLimitOptions DefaultOptions => new()
    {
        Auth = new RateLimitGroupOptions { RequestsPerMinute = 2, KeyType = "IP" },
        Crud = new RateLimitGroupOptions { RequestsPerMinute = 3, KeyType = "User" },
    };

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InvokeAsync_AllowsRequestWithinLimit()
    {
        var options = Options.Create(DefaultOptions);
        var nextCalled = false;
        var middleware = new RateLimitingMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, options);

        var context = CreateContext("/auth/request-code", "127.0.0.1");

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.NotEqual(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InvokeAsync_Returns429WhenLimitExceeded()
    {
        var options = Options.Create(DefaultOptions);
        var middleware = new RateLimitingMiddleware(_ => Task.CompletedTask, options);

        // Use unique IP to avoid interference from other tests
        var testIp = $"10.0.0.{Random.Shared.Next(1, 255)}";

        // First two requests should pass (limit is 2 for auth)
        var context1 = CreateContext("/auth/request-code", testIp);
        await middleware.InvokeAsync(context1);
        Assert.NotEqual(StatusCodes.Status429TooManyRequests, context1.Response.StatusCode);

        var context2 = CreateContext("/auth/request-code", testIp);
        await middleware.InvokeAsync(context2);
        Assert.NotEqual(StatusCodes.Status429TooManyRequests, context2.Response.StatusCode);

        // Third request should be rate limited
        var context3 = CreateContext("/auth/request-code", testIp);
        context3.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(context3);
        Assert.Equal(StatusCodes.Status429TooManyRequests, context3.Response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InvokeAsync_IncludesRetryAfterHeader_WhenLimited()
    {
        var options = Options.Create(new RateLimitOptions
        {
            Auth = new RateLimitGroupOptions { RequestsPerMinute = 1, KeyType = "IP" },
        });
        var middleware = new RateLimitingMiddleware(_ => Task.CompletedTask, options);
        var testIp = $"10.1.0.{Random.Shared.Next(1, 255)}";

        // First request passes
        var context1 = CreateContext("/auth/request-code", testIp);
        await middleware.InvokeAsync(context1);

        // Second request gets limited
        var context2 = CreateContext("/auth/request-code", testIp);
        context2.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(context2);

        Assert.Equal(StatusCodes.Status429TooManyRequests, context2.Response.StatusCode);
        Assert.Equal("60", context2.Response.Headers["Retry-After"].ToString());
    }

    private static DefaultHttpContext CreateContext(string path, string remoteIp)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = "POST";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(remoteIp);
        return context;
    }
}
