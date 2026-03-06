using MoneyTracker.Api.Configuration;
using Microsoft.Extensions.Options;

namespace MoneyTracker.Api.Tests.Unit.Configuration;

public sealed class DatabaseOptionsValidationTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_Fails_WhenConnectionStringMissingInStaging()
    {
        var validator = new DatabaseOptionsValidator(new FakeHostEnvironment("Staging"));

        var result = validator.Validate(Options.DefaultName, new DatabaseOptions
        {
            ConnectionString = string.Empty
        });

        Assert.False(result.Succeeded);
        Assert.Contains(
            "Database:ConnectionString is required for Staging and Production environments.",
            result.Failures!);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_Succeeds_WhenConnectionStringMissingInLocal()
    {
        var validator = new DatabaseOptionsValidator(new FakeHostEnvironment("Local"));

        var result = validator.Validate(Options.DefaultName, new DatabaseOptions
        {
            ConnectionString = string.Empty
        });

        Assert.True(result.Succeeded);
    }
}
