namespace MoneyTracker.Modules.Budgets.Domain;

public sealed class BudgetDomainException(string code, string message) : InvalidOperationException(message)
{
    public string Code { get; } = code;
}
