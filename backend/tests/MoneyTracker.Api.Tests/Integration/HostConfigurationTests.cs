using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MoneyTracker.Api.Tests.Integration;

public sealed class HostConfigurationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Host_BootsWithBaseConfigurationInProduction()
    {
        using var factory = new MoneyTrackerApiFactory(environmentName: "Production");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");
        response.EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        Assert.Equal("MoneyTracker.Api", configuration["Api:ServiceName"]);
        Assert.Equal("Production", configuration["Api:Environment"]);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Host_AppliesConfigurationOverrides()
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["Api:ServiceName"] = "MoneyTracker.Api.Override",
            ["Api:Environment"] = "Production"
        };

        using var factory = new MoneyTrackerApiFactory("Production", configurationOverrides);
        using var scope = factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        Assert.Equal("MoneyTracker.Api.Override", configuration["Api:ServiceName"]);
        Assert.Equal("Production", configuration["Api:Environment"]);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Host_FailsFast_WhenApiServiceNameMissing()
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["Api:ServiceName"] = string.Empty
        };

        using var factory = new MoneyTrackerApiFactory("Production", configurationOverrides);
        AssertHostFailsFast(factory, "Api:ServiceName is required.");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Host_FailsFast_WhenDatabaseConnectionStringMissingInStaging()
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["Api:Environment"] = "Staging",
            ["Database:ConnectionString"] = string.Empty
        };

        using var factory = new MoneyTrackerApiFactory("Staging", configurationOverrides);
        AssertHostFailsFast(
            factory,
            "Database:ConnectionString is required for Staging and Production environments.");
    }

    private static void AssertHostFailsFast(MoneyTrackerApiFactory factory, string expectedMessage)
    {
        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var current = exception;
        while (current is not null)
        {
            if (current is OptionsValidationException optionsException)
            {
                Assert.Contains(expectedMessage, optionsException.Message, StringComparison.Ordinal);
                return;
            }

            current = current.InnerException;
        }

        Assert.IsType<ObjectDisposedException>(exception);
    }
}
