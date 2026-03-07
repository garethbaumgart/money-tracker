using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Infrastructure;

public sealed class InMemoryBankProviderAdapter : IBankProviderAdapter
{
    public Task<CreateUserResult> CreateUserAsync(
        Guid householdId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<CreateUserResult>(cancellationToken);
        }

        var externalUserId = $"inmemory-user-{userId:N}";
        return Task.FromResult(new CreateUserResult(true, externalUserId, null, null));
    }

    public Task<CreateConsentSessionResult> CreateConsentSessionAsync(
        string externalUserId,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<CreateConsentSessionResult>(cancellationToken);
        }

        var sessionId = $"inmemory-session-{Guid.NewGuid():N}";
        var consentUrl = $"https://consent.example.com/{sessionId}";
        return Task.FromResult(new CreateConsentSessionResult(true, sessionId, consentUrl, null, null));
    }

    public Task<GetConnectionResult> GetConnectionAsync(
        string externalUserId,
        string consentSessionId,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<GetConnectionResult>(cancellationToken);
        }

        var connectionId = $"inmemory-conn-{Guid.NewGuid():N}";
        return Task.FromResult(new GetConnectionResult(
            true, connectionId, "In-Memory Bank", "active", null, null));
    }

    public Task<GetAccountsResult> GetAccountsAsync(
        string externalConnectionId,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<GetAccountsResult>(cancellationToken);
        }

        return Task.FromResult(new GetAccountsResult(
            true, Array.Empty<BankAccountInfo>(), null, null));
    }

    public Task<GetTransactionsResult> GetTransactionsAsync(
        string externalConnectionId,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<GetTransactionsResult>(cancellationToken);
        }

        // In-memory adapter returns stub transactions for testing
        var transactions = new[]
        {
            new ProviderTransaction(
                $"txn-{Guid.NewGuid():N}",
                25.50m,
                sinceUtc.AddHours(1),
                "Grocery Store Purchase"),
            new ProviderTransaction(
                $"txn-{Guid.NewGuid():N}",
                12.00m,
                sinceUtc.AddHours(2),
                "Coffee Shop")
        };

        return Task.FromResult(new GetTransactionsResult(
            true, transactions, null, null));
    }
}
