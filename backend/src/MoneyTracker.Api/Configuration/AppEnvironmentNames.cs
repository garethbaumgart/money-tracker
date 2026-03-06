namespace MoneyTracker.Api.Configuration;

internal static class AppEnvironmentNames
{
    public const string Local = "Local";
    public const string Staging = "Staging";
    public const string Production = "Production";
    public const string Testing = "Testing";

    public static string? Normalize(string? environmentName)
    {
        if (string.IsNullOrWhiteSpace(environmentName))
        {
            return null;
        }

        var normalized = environmentName.Trim();

        if (normalized.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            return Local;
        }

        if (normalized.Equals(Local, StringComparison.OrdinalIgnoreCase))
        {
            return Local;
        }

        if (normalized.Equals(Staging, StringComparison.OrdinalIgnoreCase))
        {
            return Staging;
        }

        if (normalized.Equals(Production, StringComparison.OrdinalIgnoreCase))
        {
            return Production;
        }

        if (normalized.Equals(Testing, StringComparison.OrdinalIgnoreCase))
        {
            return Testing;
        }

        return null;
    }

    public static bool RequiresDatabaseConnection(string? environmentName)
    {
        var normalized = Normalize(environmentName);
        return normalized is Staging or Production;
    }
}
