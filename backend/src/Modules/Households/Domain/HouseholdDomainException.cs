namespace MoneyTracker.Modules.Households.Domain;

public sealed class HouseholdDomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
