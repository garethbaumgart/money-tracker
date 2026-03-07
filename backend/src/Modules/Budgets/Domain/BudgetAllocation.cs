namespace MoneyTracker.Modules.Budgets.Domain;

public sealed class BudgetAllocation
{
    public BudgetAllocationId Id { get; }
    public Guid HouseholdId { get; }
    public BudgetCategoryId CategoryId { get; }
    public decimal Amount { get; private set; }
    public DateTimeOffset PeriodStartUtc { get; }
    public Guid CreatedByUserId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private BudgetAllocation(
        BudgetAllocationId id,
        Guid householdId,
        BudgetCategoryId categoryId,
        decimal amount,
        DateTimeOffset periodStartUtc,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        HouseholdId = householdId;
        CategoryId = categoryId;
        Amount = amount;
        PeriodStartUtc = periodStartUtc;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static BudgetAllocation Create(
        Guid householdId,
        BudgetCategoryId categoryId,
        decimal amount,
        DateTimeOffset periodStartUtc,
        Guid createdByUserId,
        DateTimeOffset nowUtc)
    {
        ValidateAmount(amount);

        if (!BudgetPeriod.IsPeriodStart(periodStartUtc))
        {
            throw new BudgetDomainException(
                BudgetErrors.BudgetPeriodInvalid,
                "Budget period must start at the beginning of a calendar month in UTC.");
        }

        return new BudgetAllocation(
            BudgetAllocationId.New(),
            householdId,
            categoryId,
            amount,
            periodStartUtc,
            createdByUserId,
            nowUtc,
            nowUtc);
    }

    public void UpdateAmount(decimal amount, DateTimeOffset nowUtc)
    {
        ValidateAmount(amount);
        Amount = amount;
        UpdatedAtUtc = nowUtc;
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount < 0)
        {
            throw new BudgetDomainException(
                BudgetErrors.BudgetAllocationAmountInvalid,
                "Budget allocation amount must be zero or greater.");
        }
    }
}
