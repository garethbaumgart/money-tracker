using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using MoneyTracker.Api.Tests.Component;

namespace MoneyTracker.Api.Tests.Integration;

public sealed class TransactionSyncFlowTests : IClassFixture<MoneyTrackerApiFactory>
{
    private readonly MoneyTrackerApiFactory _factory;

    public TransactionSyncFlowTests(MoneyTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task FullFlow_LinkAccount_Sync_ListTransactions_SyncedTransactionsAppear()
    {
        // P3-2-E2E-01: Link account -> sync -> list transactions -> synced transactions appear with source=Synced
        using var client = _factory.CreateClient();

        // Step 1: Authenticate
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        // Step 2: Create a household
        using var householdResponse = await client.PostAsJsonAsync(
            "/households",
            new { name = $"SyncE2E-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, householdResponse.StatusCode);
        var householdPayload = JsonNode.Parse(await householdResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(householdPayload);
        var householdId = Guid.Parse(householdPayload["id"]!.GetValue<string>());

        // Step 3: Create link session
        using var linkResponse = await client.PostAsJsonAsync(
            "/bank/link-session",
            new { householdId });
        Assert.Equal(HttpStatusCode.OK, linkResponse.StatusCode);
        var linkPayload = JsonNode.Parse(await linkResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(linkPayload);

        // Step 4: Process callback to activate the connection
        var consentUrl = linkPayload["consentUrl"]!.GetValue<string>();
        var consentSessionId = consentUrl.Split('/').Last();

        using var callbackResponse = await client.PostAsJsonAsync(
            "/bank/callback",
            new { consentSessionId });
        Assert.Equal(HttpStatusCode.OK, callbackResponse.StatusCode);
        var callbackPayload = JsonNode.Parse(await callbackResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(callbackPayload);
        Assert.Equal("Active", callbackPayload["status"]?.GetValue<string>());

        // Step 5: Trigger manual sync
        using var syncResponse = await client.PostAsJsonAsync(
            "/bank/sync",
            new { householdId });
        Assert.Equal(HttpStatusCode.OK, syncResponse.StatusCode);
        var syncPayload = JsonNode.Parse(await syncResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(syncPayload);
        var syncedCount = syncPayload["syncedCount"]!.GetValue<int>();
        Assert.True(syncedCount > 0, "Expected synced transactions from in-memory provider.");

        // Step 6: List transactions — synced transactions should appear with source=Synced
        using var listResponse = await client.GetAsync($"/transactions?householdId={householdId}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listPayload = JsonNode.Parse(await listResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(listPayload);
        var transactions = listPayload["transactions"]!.AsArray();
        Assert.True(transactions.Count > 0, "Expected at least one synced transaction.");

        // Verify all returned transactions have source=Synced
        foreach (var txn in transactions)
        {
            var txnObj = txn!.AsObject();
            Assert.Equal("Synced", txnObj["source"]?.GetValue<string>());
            Assert.False(string.IsNullOrWhiteSpace(txnObj["externalTransactionId"]?.GetValue<string>()));
        }
    }
}
