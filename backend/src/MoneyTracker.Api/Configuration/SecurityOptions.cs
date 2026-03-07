namespace MoneyTracker.Api.Configuration;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public long MaxPayloadSizeBytes { get; init; } = 1_048_576;
}
