using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MoneyTracker.Api.Configuration;

internal sealed class DatabaseOptionsValidator(IHostEnvironment hostEnvironment) : IValidateOptions<DatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        if (!AppEnvironmentNames.RequiresDatabaseConnection(hostEnvironment.EnvironmentName))
        {
            return ValidateOptionsResult.Success;
        }

        return string.IsNullOrWhiteSpace(options.ConnectionString)
            ? ValidateOptionsResult.Fail("Database:ConnectionString is required for Staging and Production environments.")
            : ValidateOptionsResult.Success;
    }
}
