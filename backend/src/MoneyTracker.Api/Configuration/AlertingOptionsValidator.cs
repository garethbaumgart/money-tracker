using Microsoft.Extensions.Options;

namespace MoneyTracker.Api.Configuration;

internal sealed class AlertingOptionsValidator : IValidateOptions<AlertingOptions>
{
    public ValidateOptionsResult Validate(string? name, AlertingOptions options)
    {
        var failures = new List<string>();

        if (options.ErrorRateThresholdPercent <= 0 || options.ErrorRateThresholdPercent > 100)
        {
            failures.Add("Alerting:ErrorRateThresholdPercent must be between 0 (exclusive) and 100 (inclusive).");
        }

        if (options.ErrorRateWindowSeconds <= 0)
        {
            failures.Add("Alerting:ErrorRateWindowSeconds must be greater than 0.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
