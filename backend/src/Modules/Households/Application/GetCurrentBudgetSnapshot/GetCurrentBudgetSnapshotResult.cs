namespace MoneyTracker.Modules.Households.Application.GetCurrentBudgetSnapshot;

public sealed class GetCurrentBudgetSnapshotResult
{
    private GetCurrentBudgetSnapshotResult(
        CurrentBudgetSnapshot? snapshot,
        string? errorCode,
        string? errorMessage)
    {
        Snapshot = snapshot;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public CurrentBudgetSnapshot? Snapshot { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess => Snapshot is not null;

    public static GetCurrentBudgetSnapshotResult Success(CurrentBudgetSnapshot snapshot)
    {
        return new GetCurrentBudgetSnapshotResult(snapshot, errorCode: null, errorMessage: null);
    }

    public static GetCurrentBudgetSnapshotResult AccessDenied()
    {
        return new GetCurrentBudgetSnapshotResult(
            snapshot: null,
            MoneyTracker.Modules.Budgets.Domain.BudgetErrors.BudgetAccessDenied,
            "User is not a member of this household.");
    }

    public static GetCurrentBudgetSnapshotResult HouseholdNotFound()
    {
        return new GetCurrentBudgetSnapshotResult(
            snapshot: null,
            MoneyTracker.Modules.Budgets.Domain.BudgetErrors.BudgetHouseholdNotFound,
            "Household not found.");
    }
}

public sealed record CurrentBudgetSnapshot(
    Guid HouseholdId,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    decimal TotalAllocated,
    decimal TotalSpent,
    decimal TotalRemaining,
    decimal UncategorizedSpent,
    BudgetCategorySnapshot[] Categories);

public sealed record BudgetCategorySnapshot(
    Guid CategoryId,
    string Name,
    decimal Allocated,
    decimal Spent,
    decimal Remaining);
