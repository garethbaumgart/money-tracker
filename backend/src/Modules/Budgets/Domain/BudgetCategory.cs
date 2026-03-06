namespace MoneyTracker.Modules.Budgets.Domain;

public sealed class BudgetCategory
{
    public const int MaxNameLength = 80;

    public BudgetCategoryId Id { get; }
    public Guid HouseholdId { get; }
    public string Name { get; }
    public string NormalizedName { get; }
    public Guid CreatedByUserId { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    private BudgetCategory(
        BudgetCategoryId id,
        Guid householdId,
        string name,
        string normalizedName,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        HouseholdId = householdId;
        Name = name;
        NormalizedName = normalizedName;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public static BudgetCategory Create(
        Guid householdId,
        string? name,
        Guid createdByUserId,
        DateTimeOffset nowUtc)
    {
        var trimmed = NormalizeName(name);
        if (trimmed.Length == 0)
        {
            throw new BudgetDomainException(
                BudgetErrors.BudgetCategoryNameRequired,
                "Category name is required.");
        }

        if (trimmed.Length > MaxNameLength)
        {
            throw new BudgetDomainException(
                BudgetErrors.ValidationError,
                $"Category name must be {MaxNameLength} characters or fewer.");
        }

        return new BudgetCategory(
            BudgetCategoryId.New(),
            householdId,
            trimmed,
            NormalizeKey(trimmed),
            createdByUserId,
            nowUtc);
    }

    public static string NormalizeName(string? name)
    {
        return name?.Trim() ?? string.Empty;
    }

    public static string NormalizeKey(string name)
    {
        return name.Trim().ToUpperInvariant();
    }
}
