using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MoneyTracker.Api.Configuration;

namespace MoneyTracker.Api.Tests.Unit.Configuration;

public sealed class PerformanceOptionsTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_Succeeds_WithDefaultValues()
    {
        var validator = new PerformanceOptionsValidator();

        var result = validator.Validate(Options.DefaultName, new PerformanceOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_Fails_WhenBudgetIsZero()
    {
        var validator = new PerformanceOptionsValidator();
        var options = new PerformanceOptions
        {
            ResponseTimeBudgets = new ResponseTimeBudgetsOptions
            {
                Auth = 0,
                Crud = 300,
                Dashboard = 500,
                Insights = 500,
                BankSync = 1000,
                Admin = 1000,
            },
        };

        var result = validator.Validate(Options.DefaultName, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure =>
            failure.Contains("Performance:ResponseTimeBudgets:Auth must be greater than 0.", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_Fails_WhenMinPoolSizeExceedsMaxPoolSize()
    {
        var validator = new PerformanceOptionsValidator();
        var options = new PerformanceOptions
        {
            ConnectionPool = new ConnectionPoolOptions
            {
                MinPoolSize = 100,
                MaxPoolSize = 10,
                ConnectionIdleLifetime = 300,
                ConnectionLifetime = 3600,
            },
        };

        var result = validator.Validate(Options.DefaultName, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure =>
            failure.Contains("MinPoolSize must not exceed MaxPoolSize", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_Fails_WhenPoolSizeIsZero()
    {
        var validator = new PerformanceOptionsValidator();
        var options = new PerformanceOptions
        {
            ConnectionPool = new ConnectionPoolOptions
            {
                MinPoolSize = 0,
                MaxPoolSize = 0,
                ConnectionIdleLifetime = 300,
                ConnectionLifetime = 3600,
            },
        };

        var result = validator.Validate(Options.DefaultName, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure =>
            failure.Contains("MinPoolSize must be at least 1", StringComparison.Ordinal));
        Assert.Contains(result.Failures!, failure =>
            failure.Contains("MaxPoolSize must be at least 1", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void BudgetsLoadedFromConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Performance:ResponseTimeBudgets:Auth"] = "100",
                ["Performance:ResponseTimeBudgets:Crud"] = "150",
                ["Performance:ResponseTimeBudgets:Dashboard"] = "250",
                ["Performance:ResponseTimeBudgets:Insights"] = "350",
                ["Performance:ResponseTimeBudgets:BankSync"] = "800",
                ["Performance:ResponseTimeBudgets:Admin"] = "900",
                ["Performance:ConnectionPool:MinPoolSize"] = "3",
                ["Performance:ConnectionPool:MaxPoolSize"] = "25",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<PerformanceOptions>()
            .Bind(config.GetSection(PerformanceOptions.SectionName));

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<PerformanceOptions>>().Value;

        Assert.Equal(100, options.ResponseTimeBudgets.Auth);
        Assert.Equal(150, options.ResponseTimeBudgets.Crud);
        Assert.Equal(250, options.ResponseTimeBudgets.Dashboard);
        Assert.Equal(350, options.ResponseTimeBudgets.Insights);
        Assert.Equal(800, options.ResponseTimeBudgets.BankSync);
        Assert.Equal(900, options.ResponseTimeBudgets.Admin);
        Assert.Equal(3, options.ConnectionPool.MinPoolSize);
        Assert.Equal(25, options.ConnectionPool.MaxPoolSize);
    }
}
