using MoneyTracker.Modules.Auth.Application.ExportUserData;

namespace MoneyTracker.Modules.Auth.Tests;

public sealed class ExportUserDataHandlerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_CollectsDataFromAllParticipants()
    {
        var handler = new ExportUserDataHandler();
        var userId = Guid.NewGuid();
        var participants = new List<ExportParticipantEntry>
        {
            new("Households", (uid, ct) => Task.FromResult<object>(new { members = new[] { uid } })),
            new("Transactions", (uid, ct) => Task.FromResult<object>(new { count = 5 })),
        };

        var result = await handler.HandleAsync(
            new ExportUserDataQuery(userId),
            participants,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data!.Count);
        Assert.True(result.Data.ContainsKey("Households"));
        Assert.True(result.Data.ContainsKey("Transactions"));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ReturnsEmptyData_WhenNoParticipants()
    {
        var handler = new ExportUserDataHandler();
        var userId = Guid.NewGuid();

        var result = await handler.HandleAsync(
            new ExportUserDataQuery(userId),
            [],
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data!);
    }
}
