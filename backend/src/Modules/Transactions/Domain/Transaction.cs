namespace MoneyTracker.Modules.Transactions.Domain;

public sealed class Transaction
{
    public TransactionId Id { get; }
    public Guid HouseholdId { get; }
    public Guid CreatedByUserId { get; }
    public decimal Amount { get; }
    public DateTimeOffset OccurredAtUtc { get; }
    public string? Description { get; }
    public Guid? CategoryId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public TransactionSource Source { get; }
    public string? ExternalTransactionId { get; }
    public Guid? BankConnectionId { get; }

    private Transaction(
        TransactionId id,
        Guid householdId,
        Guid createdByUserId,
        decimal amount,
        DateTimeOffset occurredAtUtc,
        string? description,
        Guid? categoryId,
        DateTimeOffset createdAtUtc,
        TransactionSource source,
        string? externalTransactionId,
        Guid? bankConnectionId)
    {
        Id = id;
        HouseholdId = householdId;
        CreatedByUserId = createdByUserId;
        Amount = amount;
        OccurredAtUtc = occurredAtUtc;
        Description = description;
        CategoryId = categoryId;
        CreatedAtUtc = createdAtUtc;
        Source = source;
        ExternalTransactionId = externalTransactionId;
        BankConnectionId = bankConnectionId;
    }

    public static Transaction Create(
        Guid householdId,
        Guid createdByUserId,
        decimal amount,
        DateTimeOffset occurredAtUtc,
        string? description,
        Guid? categoryId,
        DateTimeOffset nowUtc)
    {
        ValidateAmount(amount);
        var utcOccurredAt = occurredAtUtc.ToUniversalTime();
        ValidateOccurredAt(utcOccurredAt, nowUtc);

        return new Transaction(
            TransactionId.New(),
            householdId,
            createdByUserId,
            amount,
            utcOccurredAt,
            NormalizeDescription(description),
            categoryId,
            nowUtc,
            TransactionSource.Manual,
            externalTransactionId: null,
            bankConnectionId: null);
    }

    public static Transaction CreateSynced(
        Guid householdId,
        Guid bankConnectionId,
        string externalTransactionId,
        decimal amount,
        DateTimeOffset occurredAtUtc,
        string? description,
        DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(externalTransactionId))
        {
            throw new TransactionDomainException(
                TransactionErrors.ValidationError,
                "External transaction ID is required for synced transactions.");
        }

        ValidateAmount(amount);

        return new Transaction(
            TransactionId.New(),
            householdId,
            createdByUserId: Guid.Empty,
            amount,
            occurredAtUtc.ToUniversalTime(),
            NormalizeDescription(description),
            categoryId: null,
            nowUtc,
            TransactionSource.Synced,
            externalTransactionId,
            bankConnectionId);
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new TransactionDomainException(
                TransactionErrors.TransactionAmountInvalid,
                "Transaction amount must be greater than zero.");
        }
    }

    private static void ValidateOccurredAt(DateTimeOffset occurredAtUtc, DateTimeOffset nowUtc)
    {
        var min = nowUtc.Add(-TransactionPolicy.MaxPastSkew);
        var max = nowUtc.Add(TransactionPolicy.MaxFutureSkew);
        if (occurredAtUtc < min || occurredAtUtc > max)
        {
            throw new TransactionDomainException(
                TransactionErrors.TransactionDateOutOfRange,
                "Transaction date is outside the allowed range.");
        }
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return description.Trim();
    }
}
