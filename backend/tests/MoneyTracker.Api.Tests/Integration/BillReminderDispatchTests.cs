using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Api.Tests.Component;
using MoneyTracker.Modules.BillReminders.Application.DispatchDueReminders;

namespace MoneyTracker.Api.Tests.Integration;

public sealed class BillReminderDispatchTests : IClassFixture<MoneyTrackerApiFactory>
{
    private readonly MoneyTrackerApiFactory _factory;

    public BillReminderDispatchTests(MoneyTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DispatchDueReminders_SendsAndUpdatesReminderState()
    {
        using var client = _factory.CreateClient();
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        using var householdResponse = await client.PostAsJsonAsync(
            "/households",
            new { name = $"Household-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, householdResponse.StatusCode);
        var householdPayload = JsonNode.Parse(await householdResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(householdPayload);
        var householdId = Guid.Parse(householdPayload["id"]!.GetValue<string>());

        using var tokenResponse = await client.PostAsJsonAsync(
            "/notifications/device-tokens",
            new { deviceId = "device-1", token = "token-abc", platform = "ios" });
        Assert.Equal(HttpStatusCode.Created, tokenResponse.StatusCode);

        var dueDateUtc = DateTimeOffset.UtcNow.AddDays(-1);
        using var reminderResponse = await client.PostAsJsonAsync(
            $"/households/{householdId}/bill-reminders",
            new
            {
                title = "Phone bill",
                amount = 80m,
                dueDateUtc,
                cadence = "Monthly"
            });
        Assert.Equal(HttpStatusCode.Created, reminderResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<DispatchDueRemindersHandler>();
        await dispatcher.HandleAsync(CancellationToken.None);

        using var listResponse = await client.GetAsync($"/households/{householdId}/bill-reminders");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listPayload = JsonNode.Parse(await listResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(listPayload);
        var reminders = listPayload["reminders"]!.AsArray();
        Assert.Single(reminders);
        var reminder = reminders[0]!.AsObject();
        Assert.False(string.IsNullOrWhiteSpace(reminder["lastNotifiedAtUtc"]?.GetValue<string>()));
        Assert.Equal(0, reminder["dispatchAttemptCount"]?.GetValue<int>());
    }
}
