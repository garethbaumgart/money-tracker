namespace MoneyTracker.Modules.BankConnections.Domain;

public interface IBankProviderAdapter
{
    Task<CreateUserResult> CreateUserAsync(
        Guid householdId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<CreateConsentSessionResult> CreateConsentSessionAsync(
        string externalUserId,
        CancellationToken cancellationToken);

    Task<GetConnectionResult> GetConnectionAsync(
        string externalUserId,
        string consentSessionId,
        CancellationToken cancellationToken);

    Task<GetAccountsResult> GetAccountsAsync(
        string externalConnectionId,
        CancellationToken cancellationToken);

    Task<GetTransactionsResult> GetTransactionsAsync(
        string externalConnectionId,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken);
}

public sealed record CreateUserResult(bool IsSuccess, string? ExternalUserId, string? ErrorCode, string? ErrorMessage);

public sealed record CreateConsentSessionResult(
    bool IsSuccess,
    string? SessionId,
    string? ConsentUrl,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record GetConnectionResult(
    bool IsSuccess,
    string? ConnectionId,
    string? InstitutionName,
    string? Status,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record GetAccountsResult(
    bool IsSuccess,
    IReadOnlyCollection<BankAccountInfo>? Accounts,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record BankAccountInfo(
    string AccountId,
    string Name,
    string? AccountNumber,
    string? Type);

public sealed record GetTransactionsResult(
    bool IsSuccess,
    IReadOnlyCollection<ProviderTransaction>? Transactions,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record ProviderTransaction(
    string ExternalTransactionId,
    decimal Amount,
    DateTimeOffset PostedAtUtc,
    string? Description);
