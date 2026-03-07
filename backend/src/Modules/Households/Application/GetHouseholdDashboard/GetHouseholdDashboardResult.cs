using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Modules.Households.Application.GetHouseholdDashboard;

public sealed class GetHouseholdDashboardResult
{
    private GetHouseholdDashboardResult(
        HouseholdDashboard? dashboard,
        string? errorCode,
        string? errorMessage)
    {
        Dashboard = dashboard;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public HouseholdDashboard? Dashboard { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess => Dashboard is not null;

    public static GetHouseholdDashboardResult Success(HouseholdDashboard dashboard)
    {
        return new GetHouseholdDashboardResult(dashboard, errorCode: null, errorMessage: null);
    }

    public static GetHouseholdDashboardResult AccessDenied()
    {
        return new GetHouseholdDashboardResult(
            dashboard: null,
            HouseholdErrors.HouseholdAccessDenied,
            "User is not a member of this household.");
    }

    public static GetHouseholdDashboardResult HouseholdNotFound()
    {
        return new GetHouseholdDashboardResult(
            dashboard: null,
            HouseholdErrors.HouseholdNotFound,
            "Household not found.");
    }
}

public sealed record HouseholdDashboard(
    Guid HouseholdId,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    decimal TotalAllocated,
    decimal TotalSpent,
    decimal TotalRemaining,
    decimal UncategorizedSpent,
    DashboardCategorySummary[] Categories,
    DashboardTransactionSummary[] RecentTransactions);

public sealed record DashboardCategorySummary(
    Guid CategoryId,
    string Name,
    decimal Allocated,
    decimal Spent,
    decimal Remaining);

public sealed record DashboardTransactionSummary(
    Guid Id,
    decimal Amount,
    DateTimeOffset OccurredAtUtc,
    string? Description,
    Guid? CategoryId,
    string? CategoryName);
