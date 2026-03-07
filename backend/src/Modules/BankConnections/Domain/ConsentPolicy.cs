namespace MoneyTracker.Modules.BankConnections.Domain;

public static class ConsentPolicy
{
    /// <summary>
    /// Default consent duration used when the provider does not return an explicit expiry.
    /// TODO: Replace with the actual consent duration from the Basiq API response when available.
    /// </summary>
    public static readonly TimeSpan DefaultConsentDuration = TimeSpan.FromDays(90);
}
