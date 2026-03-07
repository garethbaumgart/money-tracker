using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Application.SyncTransactions;

public sealed class SyncTransactionsResult
{
    private SyncTransactionsResult(
        int syncedCount,
        int skippedCount,
        int failedConnections,
        string? errorCode,
        string? errorMessage)
    {
        SyncedCount = syncedCount;
        SkippedCount = skippedCount;
        FailedConnections = failedConnections;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public int SyncedCount { get; }

    public int SkippedCount { get; }

    public int FailedConnections { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess => ErrorCode is null;

    public static SyncTransactionsResult Success(int syncedCount, int skippedCount, int failedConnections)
    {
        return new SyncTransactionsResult(syncedCount, skippedCount, failedConnections, errorCode: null, errorMessage: null);
    }

    public static SyncTransactionsResult AccessDenied()
    {
        return new SyncTransactionsResult(
            0, 0, 0,
            BankConnectionErrors.ConnectionAccessDenied,
            "User is not a member of this household.");
    }

    public static SyncTransactionsResult HouseholdNotFound()
    {
        return new SyncTransactionsResult(
            0, 0, 0,
            BankConnectionErrors.ConnectionHouseholdNotFound,
            "Household not found.");
    }
}
