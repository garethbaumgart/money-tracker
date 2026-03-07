using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Infrastructure;

public sealed class BasiqBankProviderAdapter : IBankProviderAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BasiqBankProviderAdapter> _logger;

    private const int MaxRetries = 3;
    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    public BasiqBankProviderAdapter(
        HttpClient httpClient,
        ILogger<BasiqBankProviderAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CreateUserResult> CreateUserAsync(
        Guid householdId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        _logger.LogInformation(
            "Creating Basiq user for household={HouseholdId} user={UserId} correlationId={CorrelationId}",
            householdId,
            userId,
            correlationId);

        try
        {
            var response = await ExecuteWithRetryAsync(
                () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, "/users")
                    {
                        Content = JsonContent.Create(new { householdId, userId })
                    };
                    request.Headers.Add("X-Correlation-Id", correlationId);
                    return request;
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Basiq CreateUser failed status={StatusCode} correlationId={CorrelationId} body={Body}",
                    response.StatusCode,
                    correlationId,
                    errorBody);
                return new CreateUserResult(false, null, BankConnectionErrors.ConnectionProviderError, $"Basiq API returned {(int)response.StatusCode}.");
            }

            var result = await response.Content.ReadFromJsonAsync<BasiqUserResponse>(cancellationToken: cancellationToken);
            if (result is null || string.IsNullOrWhiteSpace(result.Id))
            {
                return new CreateUserResult(false, null, BankConnectionErrors.ConnectionProviderError, "Basiq API returned an invalid user response.");
            }

            _logger.LogInformation(
                "Basiq user created externalUserId={ExternalUserId} correlationId={CorrelationId}",
                result.Id,
                correlationId);
            return new CreateUserResult(true, result.Id, null, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Basiq CreateUser exception correlationId={CorrelationId}",
                correlationId);
            return new CreateUserResult(false, null, BankConnectionErrors.ConnectionProviderError, ex.Message);
        }
    }

    public async Task<CreateConsentSessionResult> CreateConsentSessionAsync(
        string externalUserId,
        CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        _logger.LogInformation(
            "Creating Basiq consent session for externalUserId={ExternalUserId} correlationId={CorrelationId}",
            externalUserId,
            correlationId);

        try
        {
            var response = await ExecuteWithRetryAsync(
                () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, $"/users/{externalUserId}/auth_link")
                    {
                        Content = JsonContent.Create(new { userId = externalUserId })
                    };
                    request.Headers.Add("X-Correlation-Id", correlationId);
                    return request;
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Basiq CreateConsentSession failed status={StatusCode} correlationId={CorrelationId} body={Body}",
                    response.StatusCode,
                    correlationId,
                    errorBody);
                return new CreateConsentSessionResult(false, null, null, BankConnectionErrors.ConnectionProviderError, $"Basiq API returned {(int)response.StatusCode}.");
            }

            var result = await response.Content.ReadFromJsonAsync<BasiqConsentSessionResponse>(cancellationToken: cancellationToken);
            if (result is null || string.IsNullOrWhiteSpace(result.Id) || string.IsNullOrWhiteSpace(result.Url))
            {
                return new CreateConsentSessionResult(false, null, null, BankConnectionErrors.ConnectionProviderError, "Basiq API returned an invalid consent session response.");
            }

            _logger.LogInformation(
                "Basiq consent session created sessionId={SessionId} correlationId={CorrelationId}",
                result.Id,
                correlationId);
            return new CreateConsentSessionResult(true, result.Id, result.Url, null, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Basiq CreateConsentSession exception correlationId={CorrelationId}",
                correlationId);
            return new CreateConsentSessionResult(false, null, null, BankConnectionErrors.ConnectionProviderError, ex.Message);
        }
    }

    public async Task<GetConnectionResult> GetConnectionAsync(
        string externalUserId,
        string consentSessionId,
        CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        _logger.LogInformation(
            "Fetching Basiq connection for externalUserId={ExternalUserId} session={SessionId} correlationId={CorrelationId}",
            externalUserId,
            consentSessionId,
            correlationId);

        try
        {
            var response = await ExecuteWithRetryAsync(
                () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"/users/{externalUserId}/connections");
                    request.Headers.Add("X-Correlation-Id", correlationId);
                    return request;
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Basiq GetConnection failed status={StatusCode} correlationId={CorrelationId} body={Body}",
                    response.StatusCode,
                    correlationId,
                    errorBody);

                var errorCode = (int)response.StatusCode == 410 || (int)response.StatusCode == 404
                    ? BankConnectionErrors.ConnectionSessionExpired
                    : BankConnectionErrors.ConnectionProviderError;
                return new GetConnectionResult(false, null, null, null, errorCode, $"Basiq API returned {(int)response.StatusCode}.");
            }

            var result = await response.Content.ReadFromJsonAsync<BasiqConnectionsResponse>(cancellationToken: cancellationToken);
            var connection = result?.Data?.FirstOrDefault();
            if (connection is null || string.IsNullOrWhiteSpace(connection.Id))
            {
                return new GetConnectionResult(false, null, null, null, BankConnectionErrors.ConnectionCallbackInvalid, "No connection found for this session.");
            }

            _logger.LogInformation(
                "Basiq connection retrieved connectionId={ConnectionId} status={Status} correlationId={CorrelationId}",
                connection.Id,
                connection.Status,
                correlationId);
            return new GetConnectionResult(true, connection.Id, connection.Institution?.ShortName, connection.Status, null, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Basiq GetConnection exception correlationId={CorrelationId}",
                correlationId);
            return new GetConnectionResult(false, null, null, null, BankConnectionErrors.ConnectionProviderError, ex.Message);
        }
    }

    public async Task<GetAccountsResult> GetAccountsAsync(
        string externalConnectionId,
        CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        _logger.LogInformation(
            "Fetching Basiq accounts for connectionId={ConnectionId} correlationId={CorrelationId}",
            externalConnectionId,
            correlationId);

        try
        {
            var response = await ExecuteWithRetryAsync(
                () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"/connections/{externalConnectionId}/accounts");
                    request.Headers.Add("X-Correlation-Id", correlationId);
                    return request;
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Basiq GetAccounts failed status={StatusCode} correlationId={CorrelationId} body={Body}",
                    response.StatusCode,
                    correlationId,
                    errorBody);
                return new GetAccountsResult(false, null, BankConnectionErrors.ConnectionProviderError, $"Basiq API returned {(int)response.StatusCode}.");
            }

            var result = await response.Content.ReadFromJsonAsync<BasiqAccountsResponse>(cancellationToken: cancellationToken);
            var accounts = result?.Data?
                .Select(a => new BankAccountInfo(a.Id ?? string.Empty, a.Name ?? string.Empty, a.AccountNo, a.Class?.Type))
                .ToArray() ?? [];

            _logger.LogInformation(
                "Basiq accounts retrieved count={Count} correlationId={CorrelationId}",
                accounts.Length,
                correlationId);
            return new GetAccountsResult(true, accounts, null, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Basiq GetAccounts exception correlationId={CorrelationId}",
                correlationId);
            return new GetAccountsResult(false, null, BankConnectionErrors.ConnectionProviderError, ex.Message);
        }
    }

    public async Task<GetTransactionsResult> GetTransactionsAsync(
        string externalConnectionId,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        _logger.LogInformation(
            "Fetching Basiq transactions for connectionId={ConnectionId} since={SinceUtc} correlationId={CorrelationId}",
            externalConnectionId,
            sinceUtc,
            correlationId);

        try
        {
            var sinceParam = sinceUtc.ToString("yyyy-MM-dd");
            var response = await ExecuteWithRetryAsync(
                () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get,
                        $"/users/me/transactions?filter=connection.id.eq('{externalConnectionId}'),transaction.postDate.gt('{sinceParam}')");
                    request.Headers.Add("X-Correlation-Id", correlationId);
                    return request;
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Basiq GetTransactions failed status={StatusCode} correlationId={CorrelationId} body={Body}",
                    response.StatusCode,
                    correlationId,
                    errorBody);
                return new GetTransactionsResult(false, null, BankConnectionErrors.SyncProviderError, $"Basiq API returned {(int)response.StatusCode}.");
            }

            var result = await response.Content.ReadFromJsonAsync<BasiqTransactionsResponse>(cancellationToken: cancellationToken);
            var transactions = result?.Data?
                .Where(t => !string.IsNullOrWhiteSpace(t.Id))
                .Select(t => new ProviderTransaction(
                    t.Id!,
                    decimal.TryParse(t.Amount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amount) ? Math.Abs(amount) : 0m,
                    DateTimeOffset.TryParse(t.PostDate, out var posted) ? posted : sinceUtc,
                    t.Description))
                .Where(t => t.Amount > 0)
                .ToArray() ?? [];

            _logger.LogInformation(
                "Basiq transactions retrieved count={Count} correlationId={CorrelationId}",
                transactions.Length,
                correlationId);
            return new GetTransactionsResult(true, transactions, null, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Basiq GetTransactions exception correlationId={CorrelationId}",
                correlationId);
            return new GetTransactionsResult(false, null, BankConnectionErrors.SyncProviderError, ex.Message);
        }
    }

    internal async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        var delay = InitialRetryDelay;

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(RequestTimeout);

            var request = requestFactory();

            try
            {
                var response = await _httpClient.SendAsync(request, cts.Token);

                if (attempt < MaxRetries && IsTransientFailure(response.StatusCode))
                {
                    _logger.LogWarning(
                        "Basiq transient failure status={StatusCode} attempt={Attempt}/{MaxRetries}, retrying in {Delay}ms",
                        (int)response.StatusCode,
                        attempt + 1,
                        MaxRetries,
                        delay.TotalMilliseconds);
                    response.Dispose();
                    await Task.Delay(delay, cancellationToken);
                    delay = TimeSpan.FromTicks(delay.Ticks * 2);
                    continue;
                }

                return response;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout
                if (attempt < MaxRetries)
                {
                    _logger.LogWarning(
                        "Basiq request timeout attempt={Attempt}/{MaxRetries}, retrying in {Delay}ms",
                        attempt + 1,
                        MaxRetries,
                        delay.TotalMilliseconds);
                    await Task.Delay(delay, cancellationToken);
                    delay = TimeSpan.FromTicks(delay.Ticks * 2);
                    continue;
                }

                throw new TimeoutException($"Basiq API request timed out after {MaxRetries + 1} attempts.");
            }
        }

        throw new InvalidOperationException("Retry loop exited without returning or throwing.");
    }

    private static bool IsTransientFailure(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout
            or HttpStatusCode.RequestTimeout;
    }
}

internal sealed record BasiqUserResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }
}

internal sealed record BasiqConsentSessionResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }
}

internal sealed record BasiqConnectionsResponse
{
    [JsonPropertyName("data")]
    public BasiqConnectionData[]? Data { get; init; }
}

internal sealed record BasiqConnectionData
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("institution")]
    public BasiqInstitution? Institution { get; init; }
}

internal sealed record BasiqInstitution
{
    [JsonPropertyName("shortName")]
    public string? ShortName { get; init; }
}

internal sealed record BasiqAccountsResponse
{
    [JsonPropertyName("data")]
    public BasiqAccountData[]? Data { get; init; }
}

internal sealed record BasiqAccountData
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("accountNo")]
    public string? AccountNo { get; init; }

    [JsonPropertyName("class")]
    public BasiqAccountClass? Class { get; init; }
}

internal sealed record BasiqAccountClass
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }
}

internal sealed record BasiqTransactionsResponse
{
    [JsonPropertyName("data")]
    public BasiqTransactionData[]? Data { get; init; }
}

internal sealed record BasiqTransactionData
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("amount")]
    public string? Amount { get; init; }

    [JsonPropertyName("postDate")]
    public string? PostDate { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}
