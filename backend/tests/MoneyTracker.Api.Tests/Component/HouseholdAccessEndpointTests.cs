using System.Net;
using System.Net.Http.Json;
using System.Linq;
using System.Text.Json.Nodes;
using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Api.Tests.Component;

public sealed class HouseholdAccessEndpointTests : IClassFixture<MoneyTrackerApiFactory>
{
    private readonly MoneyTrackerApiFactory _factory;

    public HouseholdAccessEndpointTests(MoneyTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task PostInviteMembers_AndAccept_HaveHouseholdMembershipEffects()
    {
        using var client = _factory.CreateClient();
        var ownerEmail = $"{Guid.NewGuid():N}@example.com";
        var memberEmail = $"{Guid.NewGuid():N}@example.com";

        var ownerToken = await AuthTestHelpers.GetAccessTokenAsync(client, ownerEmail);
        AuthTestHelpers.SetBearer(client, ownerToken);

        var createHousehold = await client.PostAsJsonAsync("/households", new { name = $"Family-{Guid.NewGuid():N}" });
        createHousehold.EnsureSuccessStatusCode();
        var createPayload = JsonNode.Parse(await createHousehold.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(createPayload);
        var householdId = createPayload["id"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(householdId));

        var inviteResponse = await client.PostAsJsonAsync($"/households/{householdId}/invite", new { inviteeEmail = memberEmail });
        inviteResponse.EnsureSuccessStatusCode();
        var invitePayload = JsonNode.Parse(await inviteResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(invitePayload);
        var invitationToken = invitePayload["invitationToken"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(invitationToken));

        var memberToken = await AuthTestHelpers.GetAccessTokenAsync(client, memberEmail);
        AuthTestHelpers.SetBearer(client, memberToken);
        using var acceptResponse = await client.PostAsync($"/households/invitations/{invitationToken}/accept", null);
        acceptResponse.EnsureSuccessStatusCode();

        using var membersResponse = await client.GetAsync($"/households/{householdId}/members");
        membersResponse.EnsureSuccessStatusCode();
        var membersPayload = JsonNode.Parse(await membersResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(membersPayload);

        var members = membersPayload["members"]?.AsArray();
        Assert.NotNull(members);
        Assert.Equal(2, members.Count);
        var roles = members.Select(member => member?["role"]?.GetValue<string>()).ToArray();
        Assert.Contains(HouseholdRole.Owner, roles);
        Assert.Contains(HouseholdRole.Member, roles);
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task PostHouseholdInvitation_ReturnsForbidden_WhenRequestingUserIsNotOwner()
    {
        using var client = _factory.CreateClient();
        var ownerEmail = $"{Guid.NewGuid():N}@example.com";
        var ownerToken = await AuthTestHelpers.GetAccessTokenAsync(client, ownerEmail);

        AuthTestHelpers.SetBearer(client, ownerToken);
        var createHousehold = await client.PostAsJsonAsync("/households", new { name = $"Family-{Guid.NewGuid():N}" });
        createHousehold.EnsureSuccessStatusCode();
        var createPayload = JsonNode.Parse(await createHousehold.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(createPayload);
        var householdId = createPayload["id"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(householdId));

        var nonOwnerEmail = $"{Guid.NewGuid():N}@example.com";
        var nonOwnerToken = await AuthTestHelpers.GetAccessTokenAsync(client, nonOwnerEmail);
        AuthTestHelpers.SetBearer(client, nonOwnerToken);
        using var inviteResponse = await client.PostAsJsonAsync(
            $"/households/{householdId}/invite",
            new { inviteeEmail = $"{Guid.NewGuid():N}@example.com" });

        Assert.Equal(HttpStatusCode.Forbidden, inviteResponse.StatusCode);

        var payload = JsonNode.Parse(await inviteResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(payload);
        Assert.Equal("household_access_denied", payload["code"]?.GetValue<string>());
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task PostHouseholdInvitation_RejectsAlreadyUsedInvitationOnSecondAcceptance()
    {
        using var client = _factory.CreateClient();
        var ownerEmail = $"{Guid.NewGuid():N}@example.com";
        var memberEmail = $"{Guid.NewGuid():N}@example.com";

        var ownerToken = await AuthTestHelpers.GetAccessTokenAsync(client, ownerEmail);
        AuthTestHelpers.SetBearer(client, ownerToken);

        var createHousehold = await client.PostAsJsonAsync("/households", new { name = $"Family-{Guid.NewGuid():N}" });
        createHousehold.EnsureSuccessStatusCode();
        var createPayload = JsonNode.Parse(await createHousehold.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(createPayload);
        var householdId = createPayload["id"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(householdId));

        var inviteResponse = await client.PostAsJsonAsync($"/households/{householdId}/invite", new { inviteeEmail = memberEmail });
        inviteResponse.EnsureSuccessStatusCode();
        var invitePayload = JsonNode.Parse(await inviteResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(invitePayload);
        var invitationToken = invitePayload["invitationToken"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(invitationToken));

        var memberToken = await AuthTestHelpers.GetAccessTokenAsync(client, memberEmail);
        AuthTestHelpers.SetBearer(client, memberToken);
        using var firstAccept = await client.PostAsync($"/households/invitations/{invitationToken}/accept", null);
        firstAccept.EnsureSuccessStatusCode();

        using var secondAccept = await client.PostAsync($"/households/invitations/{invitationToken}/accept", null);
        Assert.Equal(HttpStatusCode.Conflict, secondAccept.StatusCode);
        var secondPayload = JsonNode.Parse(await secondAccept.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(secondPayload);
        Assert.Equal("household_invitation_used", secondPayload["code"]?.GetValue<string>());
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task GetHouseholdMembers_ReturnsUnauthorized_WhenTokenMissing()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync($"/households/{Guid.NewGuid()}/members");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
