using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyTracker.Api.Configuration;
using MoneyTracker.Api.Observability;

namespace MoneyTracker.Api.Tests.Unit.Observability;

public sealed class RequestTimingMiddlewareTests
{
    private static PerformanceOptions DefaultPerformanceOptions => new()
    {
        ResponseTimeBudgets = new ResponseTimeBudgetsOptions
        {
            Auth = 200,
            Crud = 300,
            Dashboard = 500,
            Insights = 500,
            BankSync = 1000,
            Admin = 1000,
        },
    };

    private static AlertingOptions DefaultAlertingOptions => new()
    {
        ErrorRateThresholdPercent = 5,
        ErrorRateWindowSeconds = 300,
    };

    private static ErrorRateMonitor CreateErrorRateMonitor() =>
        new(new RecordingLogger<ErrorRateMonitor>(),
            Options.Create(DefaultAlertingOptions),
            TimeProvider.System);

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InvokeAsync_LogsDuration()
    {
        var logger = new RecordingLogger<RequestTimingMiddleware>();
        var options = Options.Create(DefaultPerformanceOptions);
        var env = new FakeHostEnvironment("Testing");

        var middleware = new RequestTimingMiddleware(
            next: _ => Task.CompletedTask,
            logger: logger,
            performanceOptions: options,
            hostEnvironment: env,
            errorRateMonitor: CreateErrorRateMonitor());

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/health";
        context.Items[CorrelationHeaders.CorrelationIdItemKey] = "test-corr-id";

        await middleware.InvokeAsync(context);

        Assert.Contains(logger.LogEntries, entry =>
            entry.LogLevel == LogLevel.Information
            && entry.Message.Contains("Request completed")
            && entry.Message.Contains("durationMs"));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InvokeAsync_LogsWarning_WhenBudgetExceeded()
    {
        var logger = new RecordingLogger<RequestTimingMiddleware>();
        var perfOptions = new PerformanceOptions
        {
            ResponseTimeBudgets = new ResponseTimeBudgetsOptions
            {
                Auth = 200,
                Crud = 1, // Very low budget to trigger warning
                Dashboard = 500,
                Insights = 500,
                BankSync = 1000,
                Admin = 1000,
            },
        };
        var options = Options.Create(perfOptions);
        var env = new FakeHostEnvironment("Testing");

        var middleware = new RequestTimingMiddleware(
            next: async _ => { await Task.Delay(10); },
            logger: logger,
            performanceOptions: options,
            hostEnvironment: env,
            errorRateMonitor: CreateErrorRateMonitor());

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/households/123/transactions";
        context.Items[CorrelationHeaders.CorrelationIdItemKey] = "test-corr-id";

        await middleware.InvokeAsync(context);

        Assert.Contains(logger.LogEntries, entry =>
            entry.LogLevel == LogLevel.Warning
            && entry.Message.Contains("Response time budget exceeded"));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InvokeAsync_AddsDurationHeader_InTestingEnvironment()
    {
        var logger = new RecordingLogger<RequestTimingMiddleware>();
        var options = Options.Create(DefaultPerformanceOptions);
        var env = new FakeHostEnvironment("Testing");

        var middleware = new RequestTimingMiddleware(
            next: _ => Task.CompletedTask,
            logger: logger,
            performanceOptions: options,
            hostEnvironment: env,
            errorRateMonitor: CreateErrorRateMonitor());

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/health";
        context.Items[CorrelationHeaders.CorrelationIdItemKey] = "test-corr-id";

        await middleware.InvokeAsync(context);

        Assert.True(context.Response.Headers.ContainsKey("X-Request-Duration-Ms"));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InvokeAsync_OmitsDurationHeader_InProductionEnvironment()
    {
        var logger = new RecordingLogger<RequestTimingMiddleware>();
        var options = Options.Create(DefaultPerformanceOptions);
        var env = new FakeHostEnvironment("Production");

        var middleware = new RequestTimingMiddleware(
            next: _ => Task.CompletedTask,
            logger: logger,
            performanceOptions: options,
            hostEnvironment: env,
            errorRateMonitor: CreateErrorRateMonitor());

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/health";
        context.Items[CorrelationHeaders.CorrelationIdItemKey] = "test-corr-id";

        await middleware.InvokeAsync(context);

        Assert.False(context.Response.Headers.ContainsKey("X-Request-Duration-Ms"));
    }
}

internal sealed class RecordingLogger<T> : ILogger<T>
{
    public List<LogEntry> LogEntries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        LogEntries.Add(new LogEntry(logLevel, formatter(state, exception)));
    }
}

internal sealed record LogEntry(LogLevel LogLevel, string Message);

internal sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = environmentName;

    public string ApplicationName { get; set; } = "MoneyTracker.Api.Tests";

    public string ContentRootPath { get; set; } = "/tmp";

    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
        new Microsoft.Extensions.FileProviders.NullFileProvider();
}
