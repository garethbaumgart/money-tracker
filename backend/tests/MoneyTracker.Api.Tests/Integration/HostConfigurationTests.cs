using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            ["Api:Environment"] = "Integration"
        };

        using var factory = new MoneyTrackerApiFactory("Production", configurationOverrides);
        using var scope = factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        Assert.Equal("Integration", configuration["Api:Environment"]);
    }
}
