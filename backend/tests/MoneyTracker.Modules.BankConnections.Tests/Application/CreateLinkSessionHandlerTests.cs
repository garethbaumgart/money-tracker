using MoneyTracker.Modules.BankConnections.Application.CreateLinkSession;
using MoneyTracker.Modules.BankConnections.Domain;
using MoneyTracker.Modules.BankConnections.Infrastructure;
using MoneyTracker.Modules.SharedKernel.Households;

namespace MoneyTracker.Modules.BankConnections.Tests.Application;

public sealed class CreateLinkSessionHandlerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ReturnsHouseholdNotFound_WhenHouseholdDoesNotExist()
    {
        // P3-1-UNIT-03: CreateLinkSession with missing household returns validation error
        var handler = new CreateLinkSessionHandler(
            new InMemoryBankConnectionRepository(),
            new StubBankProviderAdapter(),
            new NotFoundHouseholdAccessService(),
            new FakeTimeProvider(DateTimeOffset.Parse("2026-03-01T00:00:00Z")));

        var result = await handler.HandleAsync(
            new CreateLinkSessionCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BankConnectionErrors.ConnectionHouseholdNotFound, result.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ReturnsAccessDenied_WhenUserIsNotMember()
    {
        var handler = new CreateLinkSessionHandler(
            new InMemoryBankConnectionRepository(),
            new StubBankProviderAdapter(),
            new DeniedHouseholdAccessService(),
            new FakeTimeProvider(DateTimeOffset.Parse("2026-03-01T00:00:00Z")));

        var result = await handler.HandleAsync(
            new CreateLinkSessionCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BankConnectionErrors.ConnectionAccessDenied, result.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ReturnsSuccess_WhenAllValid()
    {
        var handler = new CreateLinkSessionHandler(
            new InMemoryBankConnectionRepository(),
            new StubBankProviderAdapter(),
            new AllowedHouseholdAccessService(),
            new FakeTimeProvider(DateTimeOffset.Parse("2026-03-01T00:00:00Z")));

        var result = await handler.HandleAsync(
            new CreateLinkSessionCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.ConsentUrl);
        Assert.Equal("https://consent.basiq.io/test-session", result.ConsentUrl);
        Assert.NotNull(result.ConnectionId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ReturnsProviderError_WhenAdapterFails()
    {
        var handler = new CreateLinkSessionHandler(
            new InMemoryBankConnectionRepository(),
            new FailingBankProviderAdapter(),
            new AllowedHouseholdAccessService(),
            new FakeTimeProvider(DateTimeOffset.Parse("2026-03-01T00:00:00Z")));

        var result = await handler.HandleAsync(
            new CreateLinkSessionCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BankConnectionErrors.ConnectionProviderError, result.ErrorCode);
    }
}

internal sealed class AllowedHouseholdAccessService : IHouseholdAccessService
{
    public Task<HouseholdAccessResult> CheckMemberAsync(
        Guid householdId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(HouseholdAccessResult.Allowed());
    }

    public Task<IReadOnlyCollection<Guid>> GetMemberIdsAsync(Guid householdId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<Guid>>(Array.Empty<Guid>());
    }
}

internal sealed class NotFoundHouseholdAccessService : IHouseholdAccessService
{
    public Task<HouseholdAccessResult> CheckMemberAsync(
        Guid householdId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(HouseholdAccessResult.NotFound());
    }

    public Task<IReadOnlyCollection<Guid>> GetMemberIdsAsync(Guid householdId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<Guid>>(Array.Empty<Guid>());
    }
}

internal sealed class DeniedHouseholdAccessService : IHouseholdAccessService
{
    public Task<HouseholdAccessResult> CheckMemberAsync(
        Guid householdId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(HouseholdAccessResult.Denied());
    }

    public Task<IReadOnlyCollection<Guid>> GetMemberIdsAsync(Guid householdId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<Guid>>(Array.Empty<Guid>());
    }
}

internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}

internal sealed class StubBankProviderAdapter : IBankProviderAdapter
{
    public Task<CreateUserResult> CreateUserAsync(Guid householdId, Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new CreateUserResult(true, "ext-user-123", null, null));
    }

    public Task<CreateConsentSessionResult> CreateConsentSessionAsync(string externalUserId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new CreateConsentSessionResult(true, "session-abc", "https://consent.basiq.io/test-session", null, null));
    }

    public Task<GetConnectionResult> GetConnectionAsync(string externalUserId, string consentSessionId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new GetConnectionResult(true, "conn-123", "Test Bank", "active", null, null));
    }

    public Task<GetAccountsResult> GetAccountsAsync(string externalConnectionId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new GetAccountsResult(true, Array.Empty<BankAccountInfo>(), null, null));
    }
}

internal sealed class FailingBankProviderAdapter : IBankProviderAdapter
{
    public Task<CreateUserResult> CreateUserAsync(Guid householdId, Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new CreateUserResult(false, null, BankConnectionErrors.ConnectionProviderError, "Provider unavailable"));
    }

    public Task<CreateConsentSessionResult> CreateConsentSessionAsync(string externalUserId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new CreateConsentSessionResult(false, null, null, BankConnectionErrors.ConnectionProviderError, "Provider unavailable"));
    }

    public Task<GetConnectionResult> GetConnectionAsync(string externalUserId, string consentSessionId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new GetConnectionResult(false, null, null, null, BankConnectionErrors.ConnectionSessionExpired, "Session expired"));
    }

    public Task<GetAccountsResult> GetAccountsAsync(string externalConnectionId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new GetAccountsResult(false, null, BankConnectionErrors.ConnectionProviderError, "Provider unavailable"));
    }
}
