using MoneyTracker.Api.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MoneyTracker.Api.Tests.Unit.Configuration;

public sealed class ApiOptionsValidationTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_Succeeds_WhenValuesAreValidForHostEnvironment()
    {
        var validator = new ApiOptionsValidator(new FakeHostEnvironment("Production"));

        var result = validator.Validate(Options.DefaultName, new ApiOptions
        {
            ServiceName = "MoneyTracker.Api",
            Environment = "Production"
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_Fails_WhenServiceNameMissing()
    {
        var validator = new ApiOptionsValidator(new FakeHostEnvironment("Production"));

        var result = validator.Validate(Options.DefaultName, new ApiOptions
        {
            ServiceName = string.Empty,
            Environment = "Production"
        });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("Api:ServiceName is required.", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_Fails_WhenEnvironmentUnsupported()
    {
        var validator = new ApiOptionsValidator(new FakeHostEnvironment("Production"));

        var result = validator.Validate(Options.DefaultName, new ApiOptions
        {
            ServiceName = "MoneyTracker.Api",
            Environment = "Sandbox"
        });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("Api:Environment must be Local (or Development), Staging, Production, or Testing.", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_Fails_WhenApiEnvironmentDoesNotMatchHostEnvironment()
    {
        var validator = new ApiOptionsValidator(new FakeHostEnvironment("Staging"));

        var result = validator.Validate(Options.DefaultName, new ApiOptions
        {
            ServiceName = "MoneyTracker.Api",
            Environment = "Production"
        });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("does not match host environment", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Development", "Local")]
    [InlineData("Local", "Development")]
    [InlineData("Staging", "staging")]
    [Trait("Category", "Unit")]
    public void Validate_Succeeds_WhenEnvironmentMatchesAfterNormalization(string hostEnvironment, string configuredEnvironment)
    {
        var validator = new ApiOptionsValidator(new FakeHostEnvironment(hostEnvironment));

        var result = validator.Validate(Options.DefaultName, new ApiOptions
        {
            ServiceName = "MoneyTracker.Api",
            Environment = configuredEnvironment
        });

        Assert.True(result.Succeeded);
    }
}

internal sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = environmentName;

    public string ApplicationName { get; set; } = "MoneyTracker.Api.Tests";

    public string ContentRootPath { get; set; } = "/tmp";

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
