using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MoneyTracker.Modules.BankConnections.Application.CreateLinkSession;
using MoneyTracker.Modules.BankConnections.Application.ProcessCallback;
using MoneyTracker.Modules.BankConnections.Domain;
using MoneyTracker.Modules.BankConnections.Infrastructure;

namespace MoneyTracker.Modules.BankConnections.Tests.Integration;

public sealed class BasiqBankProviderAdapterTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateConsentSession_AgainstStub_ReturnsConsentUrl()
    {
        // P3-1-INT-01: BasiqBankProviderAdapter creates consent session against stub
        var handler = new StubHttpHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { id = "session-1", url = "https://consent.basiq.io/session-1" }),
                System.Text.Encoding.UTF8,
                "application/json")
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://au-api.basiq.io") };
        var adapter = new BasiqBankProviderAdapter(
            httpClient,
            NullLogger<BasiqBankProviderAdapter>.Instance);

        var result = await adapter.CreateConsentSessionAsync("ext-user-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("session-1", result.SessionId);
        Assert.Equal("https://consent.basiq.io/session-1", result.ConsentUrl);

        // Verify correct parameters were sent
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Contains("/users/ext-user-1/auth_link", handler.LastRequest.RequestUri!.ToString());
        Assert.True(handler.LastRequest.Headers.Contains("X-Correlation-Id"));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateConsentSession_Timeout_RetriesWithBackoff()
    {
        // P3-1-INT-02: Basiq API timeout during consent creation
        var handler = new TimeoutThenSuccessHandler(
            timeoutsBeforeSuccess: 2,
            successResponse: new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { id = "session-2", url = "https://consent.basiq.io/session-2" }),
                    System.Text.Encoding.UTF8,
                    "application/json")
            });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://au-api.basiq.io") };
        var adapter = new BasiqBankProviderAdapter(
            httpClient,
            NullLogger<BasiqBankProviderAdapter>.Instance);

        var result = await adapter.CreateConsentSessionAsync("ext-user-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("session-2", result.SessionId);
        Assert.Equal(3, handler.AttemptCount); // 2 timeouts + 1 success
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateConsentSession_Repeated5xx_RetriesWithExponentialBackoff()
    {
        // P3-1-NF-01: Basiq adapter under repeated 5xx
        var handler = new RepeatedErrorHandler(HttpStatusCode.ServiceUnavailable);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://au-api.basiq.io") };
        var adapter = new BasiqBankProviderAdapter(
            httpClient,
            NullLogger<BasiqBankProviderAdapter>.Instance);

        var result = await adapter.CreateConsentSessionAsync("ext-user-1", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BankConnectionErrors.ConnectionProviderError, result.ErrorCode);
        // Max retries is 3, so total attempts = 4 (initial + 3 retries)
        Assert.Equal(4, handler.AttemptCount);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task FullLinkSessionThenCallback_ConnectionPersistedAsActive()
    {
        // P3-1-INT-03: Full link-session then callback flow
        var stubAdapter = new StubBankProviderAdapterForFlow();
        var repository = new InMemoryBankConnectionRepository();
        var nowUtc = DateTimeOffset.Parse("2026-03-01T00:00:00Z");
        var timeProvider = new FakeTimeProvider(nowUtc);

        // Step 1: Create link session
        var createHandler = new CreateLinkSessionHandler(
            repository,
            stubAdapter,
            new AllowedHouseholdAccessService(),
            timeProvider);

        var householdId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var createResult = await createHandler.HandleAsync(
            new CreateLinkSessionCommand(householdId, userId),
            CancellationToken.None);

        Assert.True(createResult.IsSuccess);
        Assert.NotNull(createResult.ConsentUrl);

        // Step 2: Process callback
        var callbackHandler = new ProcessCallbackHandler(
            repository,
            stubAdapter,
            timeProvider);

        var callbackResult = await callbackHandler.HandleAsync(
            new ProcessCallbackCommand("session-flow-1"),
            CancellationToken.None);

        Assert.True(callbackResult.IsSuccess);
        Assert.NotNull(callbackResult.Connection);
        Assert.Equal(BankConnectionStatus.Active, callbackResult.Connection!.Status);
        Assert.Equal("conn-flow-1", callbackResult.Connection.ExternalConnectionId);
        Assert.Equal("Flow Bank", callbackResult.Connection.InstitutionName);
    }
}

internal sealed class StubHttpHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;
    public HttpRequestMessage? LastRequest { get; private set; }

    public StubHttpHandler(HttpResponseMessage response)
    {
        _response = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(_response);
    }
}

internal sealed class TimeoutThenSuccessHandler : HttpMessageHandler
{
    private readonly int _timeoutsBeforeSuccess;
    private readonly HttpResponseMessage _successResponse;
    public int AttemptCount { get; private set; }

    public TimeoutThenSuccessHandler(int timeoutsBeforeSuccess, HttpResponseMessage successResponse)
    {
        _timeoutsBeforeSuccess = timeoutsBeforeSuccess;
        _successResponse = successResponse;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        AttemptCount++;
        if (AttemptCount <= _timeoutsBeforeSuccess)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }

        return Task.FromResult(_successResponse);
    }
}

internal sealed class RepeatedErrorHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    public int AttemptCount { get; private set; }

    public RepeatedErrorHandler(HttpStatusCode statusCode)
    {
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        AttemptCount++;
        return Task.FromResult(new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent($"{{\"error\": \"service unavailable attempt {AttemptCount}\"}}")
        });
    }
}

internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}

internal sealed class AllowedHouseholdAccessService : SharedKernel.Households.IHouseholdAccessService
{
    public Task<SharedKernel.Households.HouseholdAccessResult> CheckMemberAsync(
        Guid householdId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(SharedKernel.Households.HouseholdAccessResult.Allowed());
    }

    public Task<IReadOnlyCollection<Guid>> GetMemberIdsAsync(Guid householdId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<Guid>>(Array.Empty<Guid>());
    }
}

internal sealed class StubBankProviderAdapterForFlow : IBankProviderAdapter
{
    public Task<CreateUserResult> CreateUserAsync(Guid householdId, Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new CreateUserResult(true, "ext-user-flow-1", null, null));
    }

    public Task<CreateConsentSessionResult> CreateConsentSessionAsync(string externalUserId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new CreateConsentSessionResult(
            true, "session-flow-1", "https://consent.basiq.io/flow-1", null, null));
    }

    public Task<GetConnectionResult> GetConnectionAsync(string externalUserId, string consentSessionId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new GetConnectionResult(
            true, "conn-flow-1", "Flow Bank", "active", null, null));
    }

    public Task<GetAccountsResult> GetAccountsAsync(string externalConnectionId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new GetAccountsResult(true, Array.Empty<BankAccountInfo>(), null, null));
    }
}
