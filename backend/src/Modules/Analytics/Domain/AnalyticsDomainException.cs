namespace MoneyTracker.Modules.Analytics.Domain;

public sealed class AnalyticsDomainException : Exception
{
    public string Code { get; }

    public AnalyticsDomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}
