namespace MoneyTracker.Api.Configuration;

public sealed class ApiOptions
{
    public const string SectionName = "Api";

    public string ServiceName { get; init; } = string.Empty;

    public string Environment { get; init; } = string.Empty;
}
