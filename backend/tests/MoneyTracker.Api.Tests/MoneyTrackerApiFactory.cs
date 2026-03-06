using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MoneyTracker.Api.Tests;

public sealed class MoneyTrackerApiFactory : WebApplicationFactory<Program>
{
    private readonly string _environmentName;
    private readonly IDictionary<string, string?>? _configurationOverrides;

    public MoneyTrackerApiFactory()
        : this("Testing")
    {
    }

    internal MoneyTrackerApiFactory(
        string environmentName,
        IDictionary<string, string?>? configurationOverrides = null)
    {
        _environmentName = environmentName;
        _configurationOverrides = configurationOverrides;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environmentName);

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Api:Environment"] = _environmentName
            });

            if (_configurationOverrides is null)
            {
                return;
            }

            configBuilder.AddInMemoryCollection(_configurationOverrides);
        });
    }
}
