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
            ["Api:Environment"] = "Local"
        };

        using var factory = new MoneyTrackerApiFactory("Local", configurationOverrides);
        using var scope = factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        Assert.Equal("Local", configuration["Api:Environment"]);
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
        var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
        Assert.Contains("Api:ServiceName is required.", exception.Message, StringComparison.Ordinal);
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
        var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
        Assert.Contains("Database:ConnectionString is required for Staging and Production environments.", exception.Message, StringComparison.Ordinal);
    }
}
