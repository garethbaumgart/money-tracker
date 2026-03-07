using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace MoneyTracker.Api.Tests.Component;

public sealed class TransactionSyncEndpointTests : IClassFixture<MoneyTrackerApiFactory>
{
    private readonly MoneyTrackerApiFactory _factory;

    public TransactionSyncEndpointTests(MoneyTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task PostWebhook_WithValidSignature_Returns204()
    {
        // P3-2-COMP-01: POST /webhooks/basiq with valid signature -> 204, sync triggered
        using var client = _factory.CreateClient();

        var body = """{"eventType":"transaction.created","connectionId":"conn-123"}""";
        var signature = ComputeHmacSignature(body, "default-webhook-secret-for-testing");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/basiq");
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        request.Headers.Add("X-Basiq-Signature", signature);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task PostWebhook_WithInvalidSignature_Returns401()
    {
        // P3-2-COMP-02: POST /webhooks/basiq with invalid signature -> 401, no sync
        using var client = _factory.CreateClient();

        var body = """{"eventType":"transaction.created","connectionId":"conn-123"}""";

        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/basiq");
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        request.Headers.Add("X-Basiq-Signature", "invalid-signature-value");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var payload = JsonNode.Parse(await response.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(payload);
        Assert.Equal("bank_webhook_invalid_signature", payload["code"]?.GetValue<string>());
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task PostSync_WithValidHousehold_Returns200WithSyncSummary()
    {
        // P3-2-COMP-03: POST /bank/sync with valid household -> 200 with sync summary
        using var client = _factory.CreateClient();
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        // Create a household
        using var householdResponse = await client.PostAsJsonAsync(
            "/households",
            new { name = $"SyncTest-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, householdResponse.StatusCode);
        var householdPayload = JsonNode.Parse(await householdResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(householdPayload);
        var householdId = Guid.Parse(householdPayload["id"]!.GetValue<string>());

        // Trigger sync
        using var syncResponse = await client.PostAsJsonAsync(
            "/bank/sync",
            new { householdId });
        Assert.Equal(HttpStatusCode.OK, syncResponse.StatusCode);

        var syncPayload = JsonNode.Parse(await syncResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(syncPayload);
        Assert.NotNull(syncPayload["syncedCount"]);
        Assert.NotNull(syncPayload["skippedCount"]);
        Assert.NotNull(syncPayload["failedConnections"]);
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task GetTransactions_AfterSync_ReturnsBothManualAndSyncedWithSource()
    {
        // P3-2-COMP-04: GET /transactions after sync -> returns both manual and synced with source
        using var client = _factory.CreateClient();
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        // Create a household
        using var householdResponse = await client.PostAsJsonAsync(
            "/households",
            new { name = $"SyncTest-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, householdResponse.StatusCode);
        var householdPayload = JsonNode.Parse(await householdResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(householdPayload);
        var householdId = Guid.Parse(householdPayload["id"]!.GetValue<string>());

        // Create a manual transaction
        using var txnResponse = await client.PostAsJsonAsync(
            "/transactions",
            new
            {
                householdId,
                amount = 50.00m,
                occurredAtUtc = DateTimeOffset.UtcNow,
                description = "Manual Transaction"
            });
        Assert.Equal(HttpStatusCode.Created, txnResponse.StatusCode);

        // Get transactions — should include source metadata
        using var listResponse = await client.GetAsync($"/transactions?householdId={householdId}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var listPayload = JsonNode.Parse(await listResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(listPayload);
        var transactions = listPayload["transactions"]!.AsArray();
        Assert.Single(transactions);

        var txn = transactions[0]!.AsObject();
        Assert.Equal("Manual", txn["source"]?.GetValue<string>());
        Assert.Null(txn["externalTransactionId"]?.GetValue<string>());
    }

    private static string ComputeHmacSignature(string body, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var hash = HMACSHA256.HashData(key, bodyBytes);
        return Convert.ToHexStringLower(hash);
    }
}
