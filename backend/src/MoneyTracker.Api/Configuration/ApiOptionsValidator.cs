using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MoneyTracker.Api.Configuration;

internal sealed class ApiOptionsValidator(IHostEnvironment hostEnvironment) : IValidateOptions<ApiOptions>
{
    public ValidateOptionsResult Validate(string? name, ApiOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ServiceName))
        {
            failures.Add("Api:ServiceName is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Environment))
        {
            failures.Add("Api:Environment is required.");
        }
        else
        {
            var normalizedApiEnvironment = AppEnvironmentNames.Normalize(options.Environment);
            if (normalizedApiEnvironment is null)
            {
                failures.Add("Api:Environment must be Local (or Development), Staging, Production, or Testing.");
            }
            else
            {
                var normalizedHostEnvironment = AppEnvironmentNames.Normalize(hostEnvironment.EnvironmentName);
                if (normalizedHostEnvironment is not null && !string.Equals(normalizedApiEnvironment, normalizedHostEnvironment, StringComparison.Ordinal))
                {
                    failures.Add($"Api:Environment '{options.Environment}' does not match host environment '{hostEnvironment.EnvironmentName}'.");
                }
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
