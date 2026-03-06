namespace MoneyTracker.Modules.Transactions.Domain;

public static class TransactionPolicy
{
    public static readonly TimeSpan MaxFutureSkew = TimeSpan.FromDays(1);
    public static readonly TimeSpan MaxPastSkew = TimeSpan.FromDays(730);
}
