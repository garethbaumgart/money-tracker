using MoneyTracker.Modules.Analytics.Application.GetActivationFunnel;
using MoneyTracker.Modules.Analytics.Domain;
using MoneyTracker.Modules.Analytics.Infrastructure;

namespace MoneyTracker.Modules.Analytics.Tests;

public sealed class GetActivationFunnelHandlerTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-10T12:00:00Z");

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ComputesStageCounts()
    {
        // P5-2-UNIT-04: GetActivationFunnelHandler computes stage counts correctly
        var repository = new InMemoryActivationEventRepository();
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        await SeedEvent(repository, user1, ActivationMilestone.SignupCompleted, NowUtc.AddDays(-5));
        await SeedEvent(repository, user2, ActivationMilestone.SignupCompleted, NowUtc.AddDays(-4));
        await SeedEvent(repository, user1, ActivationMilestone.HouseholdCreated, NowUtc.AddDays(-4));

        var handler = new GetActivationFunnelHandler(repository, new StubTimeProvider(NowUtc));
        var result = await handler.HandleAsync(
            new GetActivationFunnelQuery(PeriodDays: 30),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.TotalUsers);

        var signupStage = result.Stages.First(s => s.Milestone == "signup_completed");
        Assert.Equal(2, signupStage.UserCount);
        Assert.Equal(1.0, signupStage.ConversionRate);

        var householdStage = result.Stages.First(s => s.Milestone == "household_created");
        Assert.Equal(1, householdStage.UserCount);
        Assert.Equal(0.5, householdStage.ConversionRate);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ComputesDropOffRates()
    {
        // P5-2-UNIT-05: Drop-off rates computed correctly between consecutive stages
        var repository = new InMemoryActivationEventRepository();

        // 10 users do signup, 8 do household_created, 4 do partner_invited
        for (var i = 0; i < 10; i++)
        {
            var uid = Guid.NewGuid();
            await SeedEvent(repository, uid, ActivationMilestone.SignupCompleted, NowUtc.AddDays(-5));
            if (i < 8)
                await SeedEvent(repository, uid, ActivationMilestone.HouseholdCreated, NowUtc.AddDays(-4));
            if (i < 4)
                await SeedEvent(repository, uid, ActivationMilestone.PartnerInvited, NowUtc.AddDays(-3));
        }

        var handler = new GetActivationFunnelHandler(repository, new StubTimeProvider(NowUtc));
        var result = await handler.HandleAsync(
            new GetActivationFunnelQuery(PeriodDays: 30),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var signupStage = result.Stages.First(s => s.Milestone == "signup_completed");
        Assert.Equal(0.0, signupStage.DropOffRate); // first stage has no drop-off

        var householdStage = result.Stages.First(s => s.Milestone == "household_created");
        Assert.Equal(0.2, householdStage.DropOffRate); // 1 - (8/10) = 0.2

        var partnerStage = result.Stages.First(s => s.Milestone == "partner_invited");
        Assert.Equal(0.5, partnerStage.DropOffRate); // 1 - (4/8) = 0.5
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_CohortGroupingByIsoWeek()
    {
        // P5-2-UNIT-06: Cohort grouping by ISO signup week
        var repository = new InMemoryActivationEventRepository();

        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var user3 = Guid.NewGuid();

        // Week 10 signups (Mar 2-8, 2026)
        await SeedEvent(repository, user1, ActivationMilestone.SignupCompleted,
            DateTimeOffset.Parse("2026-03-02T10:00:00Z"));
        await SeedEvent(repository, user2, ActivationMilestone.SignupCompleted,
            DateTimeOffset.Parse("2026-03-04T10:00:00Z"));

        // Week 9 signup (Feb 23-Mar 1, 2026)
        await SeedEvent(repository, user3, ActivationMilestone.SignupCompleted,
            DateTimeOffset.Parse("2026-02-25T10:00:00Z"));

        // user1 has paid_conversion
        await SeedEvent(repository, user1, ActivationMilestone.PaidConversion,
            DateTimeOffset.Parse("2026-03-09T10:00:00Z"));

        var handler = new GetActivationFunnelHandler(repository, new StubTimeProvider(NowUtc));
        var result = await handler.HandleAsync(
            new GetActivationFunnelQuery(PeriodDays: 30),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Cohorts.Count);

        // Verify week 10 cohort: 2 signups, 1 paid = 0.5 rate
        var week10 = result.Cohorts.FirstOrDefault(c => c.SignupCount == 2);
        Assert.NotNull(week10);
        Assert.Equal(0.5, week10.PaidConversionRate);

        // Verify week 9 cohort: 1 signup, 0 paid = 0.0 rate
        var week9 = result.Cohorts.FirstOrDefault(c => c.SignupCount == 1);
        Assert.NotNull(week9);
        Assert.Equal(0.0, week9.PaidConversionRate);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_FiltersByPlatform()
    {
        // P5-2-UNIT-08: Funnel query filters by platform
        var repository = new InMemoryActivationEventRepository();

        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        await SeedEvent(repository, user1, ActivationMilestone.SignupCompleted, NowUtc.AddDays(-5), "ios");
        await SeedEvent(repository, user2, ActivationMilestone.SignupCompleted, NowUtc.AddDays(-4), "android");

        var handler = new GetActivationFunnelHandler(repository, new StubTimeProvider(NowUtc));
        var result = await handler.HandleAsync(
            new GetActivationFunnelQuery(PeriodDays: 30, Platform: "ios"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.TotalUsers);

        var signupStage = result.Stages.First(s => s.Milestone == "signup_completed");
        Assert.Equal(1, signupStage.UserCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_FiltersByRegion()
    {
        // P5-2-UNIT-09: Funnel query filters by region
        var repository = new InMemoryActivationEventRepository();

        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        await SeedEvent(repository, user1, ActivationMilestone.SignupCompleted, NowUtc.AddDays(-5), "ios", "AU");
        await SeedEvent(repository, user2, ActivationMilestone.SignupCompleted, NowUtc.AddDays(-4), "ios", "NZ");

        var handler = new GetActivationFunnelHandler(repository, new StubTimeProvider(NowUtc));
        var result = await handler.HandleAsync(
            new GetActivationFunnelQuery(PeriodDays: 30, Region: "AU"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.TotalUsers);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ZeroPreviousCount_DropOffRateIsZero()
    {
        // P5-2-UNIT-10: Drop-off rate handles zero previous stage count
        var repository = new InMemoryActivationEventRepository();

        var handler = new GetActivationFunnelHandler(repository, new StubTimeProvider(NowUtc));
        var result = await handler.HandleAsync(
            new GetActivationFunnelQuery(PeriodDays: 30),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.TotalUsers);

        // All stages should have 0 counts and 0 drop-off rates
        foreach (var stage in result.Stages)
        {
            Assert.Equal(0, stage.UserCount);
            Assert.Equal(0.0, stage.DropOffRate);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_Returns12Stages()
    {
        var repository = new InMemoryActivationEventRepository();
        var handler = new GetActivationFunnelHandler(repository, new StubTimeProvider(NowUtc));
        var result = await handler.HandleAsync(
            new GetActivationFunnelQuery(PeriodDays: 30),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(12, result.Stages.Count);
        Assert.Equal("signup_completed", result.Stages.First().Milestone);
        Assert.Equal("paid_conversion", result.Stages.Last().Milestone);
    }

    private static async Task SeedEvent(
        InMemoryActivationEventRepository repository,
        Guid userId,
        ActivationMilestone milestone,
        DateTimeOffset occurredAtUtc,
        string platform = "backend",
        string? region = null)
    {
        var evt = ActivationEvent.Create(
            userId, milestone, householdId: null, platform, region,
            metadata: null, occurredAtUtc, occurredAtUtc);
        await repository.AddAsync(evt, CancellationToken.None);
    }
}
