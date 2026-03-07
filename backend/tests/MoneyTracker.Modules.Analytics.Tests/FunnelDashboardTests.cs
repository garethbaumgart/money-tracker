using MoneyTracker.Modules.Analytics.Application.GetFunnelReport;
using MoneyTracker.Modules.Analytics.Application.GetRetentionCohorts;
using MoneyTracker.Modules.Analytics.Application.GetRevenueMetrics;
using MoneyTracker.Modules.Analytics.Domain;
using MoneyTracker.Modules.Analytics.Infrastructure;
using MoneyTracker.Modules.Subscriptions.Domain;
using MoneyTracker.Modules.Subscriptions.Infrastructure;

namespace MoneyTracker.Modules.Analytics.Tests;

public sealed class FunnelDashboardTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-10T12:00:00Z");

    #region FunnelReport Tests

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeStages_ConversionRateCalculation()
    {
        // AC-1: Conversion rate = count at stage / count at previous stage
        // Test: 840/1200 = 0.70
        var events = new List<ActivationEvent>();

        // Create 1200 users at signup stage
        var userIds = Enumerable.Range(0, 1200).Select(_ => Guid.NewGuid()).ToArray();
        foreach (var uid in userIds)
        {
            events.Add(CreateEvent(uid, ActivationMilestone.SignupCompleted, NowUtc.AddDays(-5)));
        }

        // 840 users at onboarding_complete (HouseholdCreated)
        foreach (var uid in userIds.Take(840))
        {
            events.Add(CreateEvent(uid, ActivationMilestone.HouseholdCreated, NowUtc.AddDays(-4)));
        }

        var stages = FunnelDataAggregator.ComputeStages(events);

        Assert.Equal(7, stages.Count);

        var signupStage = stages[0];
        Assert.Equal("signup", signupStage.Name);
        Assert.Equal(1200, signupStage.Count);
        Assert.Equal(1.0, signupStage.ConversionRate);
        Assert.Equal(0.0, signupStage.DropOffRate);

        var onboardingStage = stages[1];
        Assert.Equal("onboarding_complete", onboardingStage.Name);
        Assert.Equal(840, onboardingStage.Count);
        Assert.Equal(0.70, onboardingStage.ConversionRate);
        Assert.Equal(0.30, onboardingStage.DropOffRate);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeStages_Returns7FunnelStages()
    {
        // AC-2: 7 funnel stages: signup through paid_conversion
        var stages = FunnelDataAggregator.ComputeStages([]);

        Assert.Equal(7, stages.Count);
        Assert.Equal("signup", stages[0].Name);
        Assert.Equal("onboarding_complete", stages[1].Name);
        Assert.Equal("first_transaction", stages[2].Name);
        Assert.Equal("bank_link", stages[3].Name);
        Assert.Equal("paywall_view", stages[4].Name);
        Assert.Equal("trial_start", stages[5].Name);
        Assert.Equal("paid_conversion", stages[6].Name);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeTopDropOffs_RankedByLostUserCount()
    {
        // AC-3: Top 3 drop-offs ranked by lost user count
        var events = new List<ActivationEvent>();
        var userIds = Enumerable.Range(0, 1000).Select(_ => Guid.NewGuid()).ToArray();

        // Signup: 1000
        foreach (var uid in userIds) events.Add(CreateEvent(uid, ActivationMilestone.SignupCompleted, NowUtc.AddDays(-7)));
        // Onboarding: 800 (lost 200)
        foreach (var uid in userIds.Take(800)) events.Add(CreateEvent(uid, ActivationMilestone.HouseholdCreated, NowUtc.AddDays(-6)));
        // FirstTransaction: 500 (lost 300)
        foreach (var uid in userIds.Take(500)) events.Add(CreateEvent(uid, ActivationMilestone.FirstTransactionCreated, NowUtc.AddDays(-5)));
        // BankLink: 400 (lost 100)
        foreach (var uid in userIds.Take(400)) events.Add(CreateEvent(uid, ActivationMilestone.BankLinkCompleted, NowUtc.AddDays(-4)));
        // PaywallView: 200 (lost 200)
        foreach (var uid in userIds.Take(200)) events.Add(CreateEvent(uid, ActivationMilestone.PaywallViewed, NowUtc.AddDays(-3)));
        // TrialStart: 150 (lost 50)
        foreach (var uid in userIds.Take(150)) events.Add(CreateEvent(uid, ActivationMilestone.TrialStarted, NowUtc.AddDays(-2)));
        // PaidConversion: 100 (lost 50)
        foreach (var uid in userIds.Take(100)) events.Add(CreateEvent(uid, ActivationMilestone.PaidConversion, NowUtc.AddDays(-1)));

        var stages = FunnelDataAggregator.ComputeStages(events);
        var topDropOffs = FunnelDataAggregator.ComputeTopDropOffs(stages);

        Assert.Equal(3, topDropOffs.Count);

        // Highest drop-off: onboarding_complete -> first_transaction (300 lost)
        Assert.Equal("onboarding_complete", topDropOffs[0].FromStage);
        Assert.Equal("first_transaction", topDropOffs[0].ToStage);
        Assert.Equal(300, topDropOffs[0].LostUsers);

        // Second: signup -> onboarding_complete (200 lost) OR bank_link -> paywall_view (200 lost)
        // Both have 200 lost users - order depends on stable sort
        Assert.Equal(200, topDropOffs[1].LostUsers);
        Assert.Equal(200, topDropOffs[2].LostUsers);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetFunnelReport_TrendsCalculation_WoW()
    {
        // AC-4: WoW trend = (thisWeek - lastWeek) / lastWeek
        // Test: 1200 this week vs 1071 last week = ~12% increase
        var repository = new InMemoryActivationEventRepository();

        // Last week: 1071 signups (2026-02-24 to 2026-03-03)
        for (var i = 0; i < 1071; i++)
        {
            await SeedEvent(repository, Guid.NewGuid(), ActivationMilestone.SignupCompleted,
                NowUtc.AddDays(-10)); // Falls in last week relative to the period
        }

        // This week: 1200 signups (2026-03-03 to 2026-03-10)
        for (var i = 0; i < 1200; i++)
        {
            await SeedEvent(repository, Guid.NewGuid(), ActivationMilestone.SignupCompleted,
                NowUtc.AddDays(-3)); // Falls in this week
        }

        var aggregator = new FunnelDataAggregator(repository, new StubTimeProvider(NowUtc));
        var report = await aggregator.GetFunnelReportAsync(
            NowUtc.AddDays(-7), NowUtc, CancellationToken.None);

        Assert.NotNull(report.Trends.WeekOverWeek);
        // (1200 - 1071) / 1071 = 0.1204
        Assert.Equal(0.1204, report.Trends.WeekOverWeek!.Value, 4);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetFunnelReport_TrendsCalculation_ZeroLastWeek_ReturnsNull()
    {
        // AC-4 edge case: when last week has 0 signups, WoW is null
        var repository = new InMemoryActivationEventRepository();

        // Only this week signups
        for (var i = 0; i < 100; i++)
        {
            await SeedEvent(repository, Guid.NewGuid(), ActivationMilestone.SignupCompleted,
                NowUtc.AddDays(-3));
        }

        var aggregator = new FunnelDataAggregator(repository, new StubTimeProvider(NowUtc));
        var report = await aggregator.GetFunnelReportAsync(
            NowUtc.AddDays(-7), NowUtc, CancellationToken.None);

        Assert.Null(report.Trends.WeekOverWeek);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeOverallConversion_CalculatesCorrectly()
    {
        var events = new List<ActivationEvent>();
        var userIds = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToArray();

        foreach (var uid in userIds)
            events.Add(CreateEvent(uid, ActivationMilestone.SignupCompleted, NowUtc.AddDays(-5)));
        foreach (var uid in userIds.Take(10))
            events.Add(CreateEvent(uid, ActivationMilestone.PaidConversion, NowUtc.AddDays(-1)));

        var stages = FunnelDataAggregator.ComputeStages(events);
        var overall = FunnelDataAggregator.ComputeOverallConversion(stages);

        Assert.Equal(0.10, overall);
    }

    #endregion

    #region Retention Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RetentionCalculator_D1D7Rates()
    {
        // AC-5: D1/D7 retention rates from known data
        var repository = new InMemoryActivationEventRepository();
        var signupDate = NowUtc.AddDays(-14);

        // 10 users signed up 14 days ago
        var userIds = new Guid[10];
        for (var i = 0; i < 10; i++)
        {
            userIds[i] = Guid.NewGuid();
            await SeedEvent(repository, userIds[i], ActivationMilestone.SignupCompleted, signupDate);
        }

        // 8 users had activity on D+1 (HouseholdCreated)
        for (var i = 0; i < 8; i++)
        {
            await SeedEvent(repository, userIds[i], ActivationMilestone.HouseholdCreated, signupDate.AddDays(1));
        }

        // 5 users had activity on D+7 (FirstTransactionCreated)
        for (var i = 0; i < 5; i++)
        {
            await SeedEvent(repository, userIds[i], ActivationMilestone.FirstTransactionCreated, signupDate.AddDays(7));
        }

        var calculator = new RetentionCalculator(repository);
        var cohorts = await calculator.GetRetentionCohortsAsync(8, NowUtc, CancellationToken.None);

        Assert.Single(cohorts);
        var cohort = cohorts[0];
        Assert.Equal(10, cohort.Signups);
        Assert.Equal(0.8, cohort.D1);
        Assert.Equal(0.5, cohort.D7);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RetentionCalculator_NullForElapsedPeriods()
    {
        // AC-6: Retention null for periods that haven't elapsed
        var repository = new InMemoryActivationEventRepository();
        var recentSignup = NowUtc.AddDays(-2); // Only 2 days ago

        for (var i = 0; i < 5; i++)
        {
            await SeedEvent(repository, Guid.NewGuid(), ActivationMilestone.SignupCompleted, recentSignup);
        }

        var calculator = new RetentionCalculator(repository);
        var cohorts = await calculator.GetRetentionCohortsAsync(8, NowUtc, CancellationToken.None);

        Assert.Single(cohorts);
        var cohort = cohorts[0];
        Assert.NotNull(cohort.D1); // D1 has elapsed (2 > 1)
        Assert.Null(cohort.D7);    // D7 hasn't elapsed (2 < 7)
        Assert.Null(cohort.D14);   // D14 hasn't elapsed
        Assert.Null(cohort.D30);   // D30 hasn't elapsed
    }

    #endregion

    #region Revenue Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RevenueCalculator_MrrWithMonthlyAndAnnualSubs()
    {
        // AC-7: MRR with monthly + annual subscriptions
        var repository = new InMemorySubscriptionRepository();

        // Monthly sub: $9.99/month
        var monthlySub = Subscription.CreateTrial(
            Guid.NewGuid(), $"user-monthly-{Guid.NewGuid()}", "mt_premium_monthly",
            NowUtc.AddDays(-30), NowUtc.AddDays(30), NowUtc.AddDays(-30));
        monthlySub.Activate(NowUtc.AddDays(-30), NowUtc.AddDays(30), NowUtc.AddDays(-30), "evt-1", NowUtc.AddDays(-29));
        await repository.AddAsync(monthlySub, CancellationToken.None);

        // Annual sub: $99.99/year = $8.33/month
        var annualSub = Subscription.CreateTrial(
            Guid.NewGuid(), $"user-annual-{Guid.NewGuid()}", "mt_premium_annual",
            NowUtc.AddDays(-30), NowUtc.AddDays(335), NowUtc.AddDays(-30));
        annualSub.Activate(NowUtc.AddDays(-30), NowUtc.AddDays(335), NowUtc.AddDays(-30), "evt-2", NowUtc.AddDays(-29));
        await repository.AddAsync(annualSub, CancellationToken.None);

        var calculator = new RevenueCalculator(repository);
        var metrics = await calculator.GetRevenueMetricsAsync(NowUtc, CancellationToken.None);

        // MRR = 9.99 + (99.99/12) = 9.99 + 8.33 = 18.32
        Assert.Equal(18.32m, metrics.Mrr);
        Assert.Equal(2, metrics.ActiveSubscribers);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RevenueCalculator_ArpuCalculation()
    {
        // AC-7: ARPU = MRR / active subscribers
        var repository = new InMemorySubscriptionRepository();

        for (var i = 0; i < 3; i++)
        {
            var sub = Subscription.CreateTrial(
                Guid.NewGuid(), $"user-{i}-{Guid.NewGuid()}", "mt_premium_monthly",
                NowUtc.AddDays(-30), NowUtc.AddDays(30), NowUtc.AddDays(-30));
            sub.Activate(NowUtc.AddDays(-30), NowUtc.AddDays(30), NowUtc.AddDays(-30), $"evt-{i}", NowUtc.AddDays(-29));
            await repository.AddAsync(sub, CancellationToken.None);
        }

        var calculator = new RevenueCalculator(repository);
        var metrics = await calculator.GetRevenueMetricsAsync(NowUtc, CancellationToken.None);

        // ARPU = 29.97 / 3 = 9.99
        Assert.Equal(9.99m, metrics.Arpu);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RevenueCalculator_ChurnRate()
    {
        // AC-7: Churn rate = cancellations in period / active at start
        var repository = new InMemorySubscriptionRepository();

        // 4 active subs
        for (var i = 0; i < 4; i++)
        {
            var sub = Subscription.CreateTrial(
                Guid.NewGuid(), $"user-active-{i}-{Guid.NewGuid()}", "mt_premium_monthly",
                NowUtc.AddDays(-60), NowUtc.AddDays(30), NowUtc.AddDays(-60));
            sub.Activate(NowUtc.AddDays(-60), NowUtc.AddDays(30), NowUtc.AddDays(-60), $"evt-a-{i}", NowUtc.AddDays(-59));
            await repository.AddAsync(sub, CancellationToken.None);
        }

        // 1 cancelled sub (within last 30 days)
        var cancelledSub = Subscription.CreateTrial(
            Guid.NewGuid(), $"user-cancelled-{Guid.NewGuid()}", "mt_premium_monthly",
            NowUtc.AddDays(-60), NowUtc.AddDays(30), NowUtc.AddDays(-60));
        cancelledSub.Activate(NowUtc.AddDays(-60), NowUtc.AddDays(30), NowUtc.AddDays(-60), "evt-c-1", NowUtc.AddDays(-59));
        cancelledSub.Cancel(NowUtc.AddDays(-10), "evt-c-2", NowUtc.AddDays(-10));
        await repository.AddAsync(cancelledSub, CancellationToken.None);

        var calculator = new RevenueCalculator(repository);
        var metrics = await calculator.GetRevenueMetricsAsync(NowUtc, CancellationToken.None);

        // Active at start = 4 (current active) + 1 (cancelled in period) = 5
        // Churn = 1 / 5 = 0.2
        Assert.Equal(0.2, metrics.ChurnRate);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RevenueCalculator_Ltv()
    {
        // AC-7: LTV = ARPU / churnRate
        var repository = new InMemorySubscriptionRepository();

        // 9 active, 1 cancelled in period
        for (var i = 0; i < 9; i++)
        {
            var sub = Subscription.CreateTrial(
                Guid.NewGuid(), $"user-ltv-{i}-{Guid.NewGuid()}", "mt_premium_monthly",
                NowUtc.AddDays(-60), NowUtc.AddDays(30), NowUtc.AddDays(-60));
            sub.Activate(NowUtc.AddDays(-60), NowUtc.AddDays(30), NowUtc.AddDays(-60), $"evt-l-{i}", NowUtc.AddDays(-59));
            await repository.AddAsync(sub, CancellationToken.None);
        }

        var cancelledSub = Subscription.CreateTrial(
            Guid.NewGuid(), $"user-ltv-cancelled-{Guid.NewGuid()}", "mt_premium_monthly",
            NowUtc.AddDays(-60), NowUtc.AddDays(30), NowUtc.AddDays(-60));
        cancelledSub.Activate(NowUtc.AddDays(-60), NowUtc.AddDays(30), NowUtc.AddDays(-60), "evt-lc-1", NowUtc.AddDays(-59));
        cancelledSub.Cancel(NowUtc.AddDays(-5), "evt-lc-2", NowUtc.AddDays(-5));
        await repository.AddAsync(cancelledSub, CancellationToken.None);

        var calculator = new RevenueCalculator(repository);
        var metrics = await calculator.GetRevenueMetricsAsync(NowUtc, CancellationToken.None);

        // ARPU = 89.91 / 9 = 9.99
        // Churn = 1 / 10 = 0.1
        // LTV = 9.99 / 0.1 = 99.90
        Assert.NotNull(metrics.EstimatedLtv);
        Assert.Equal(99.90m, metrics.EstimatedLtv!.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RevenueCalculator_LtvNull_WhenZeroChurn()
    {
        // AC-7: LTV = null when churnRate = 0
        var repository = new InMemorySubscriptionRepository();

        var sub = Subscription.CreateTrial(
            Guid.NewGuid(), $"user-nochurn-{Guid.NewGuid()}", "mt_premium_monthly",
            NowUtc.AddDays(-60), NowUtc.AddDays(30), NowUtc.AddDays(-60));
        sub.Activate(NowUtc.AddDays(-60), NowUtc.AddDays(30), NowUtc.AddDays(-60), "evt-nc-1", NowUtc.AddDays(-59));
        await repository.AddAsync(sub, CancellationToken.None);

        var calculator = new RevenueCalculator(repository);
        var metrics = await calculator.GetRevenueMetricsAsync(NowUtc, CancellationToken.None);

        Assert.Equal(0.0, metrics.ChurnRate);
        Assert.Null(metrics.EstimatedLtv);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RevenueCalculator_TrialUsersCount()
    {
        // AC-7: Trial users counted separately
        var repository = new InMemorySubscriptionRepository();

        // 1 active sub
        var activeSub = Subscription.CreateTrial(
            Guid.NewGuid(), $"user-a-{Guid.NewGuid()}", "mt_premium_monthly",
            NowUtc.AddDays(-30), NowUtc.AddDays(30), NowUtc.AddDays(-30));
        activeSub.Activate(NowUtc.AddDays(-30), NowUtc.AddDays(30), NowUtc.AddDays(-30), "evt-a", NowUtc.AddDays(-29));
        await repository.AddAsync(activeSub, CancellationToken.None);

        // 2 trial subs
        for (var i = 0; i < 2; i++)
        {
            var trialSub = Subscription.CreateTrial(
                Guid.NewGuid(), $"user-t-{i}-{Guid.NewGuid()}", "mt_premium_monthly",
                NowUtc.AddDays(-5), NowUtc.AddDays(9), NowUtc.AddDays(-5));
            await repository.AddAsync(trialSub, CancellationToken.None);
        }

        var calculator = new RevenueCalculator(repository);
        var metrics = await calculator.GetRevenueMetricsAsync(NowUtc, CancellationToken.None);

        Assert.Equal(1, metrics.ActiveSubscribers);
        Assert.Equal(2, metrics.TrialUsers);
    }

    #endregion

    #region GetMonthlyEquivalent Tests

    [Fact]
    [Trait("Category", "Unit")]
    public void GetMonthlyEquivalent_MonthlyProduct()
    {
        var result = RevenueCalculator.GetMonthlyEquivalent("mt_premium_monthly");
        Assert.Equal(9.99m, result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GetMonthlyEquivalent_AnnualProduct()
    {
        var result = RevenueCalculator.GetMonthlyEquivalent("mt_premium_annual");
        Assert.Equal(8.33m, result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GetMonthlyEquivalent_UnknownProduct_ReturnsDefault()
    {
        var result = RevenueCalculator.GetMonthlyEquivalent("unknown_product");
        Assert.Equal(9.99m, result);
    }

    #endregion

    #region Handler Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetFunnelReportHandler_InvalidPeriod_ReturnsFailure()
    {
        var aggregator = new FunnelDataAggregator(
            new InMemoryActivationEventRepository(), new StubTimeProvider(NowUtc));
        var handler = new GetFunnelReportHandler(aggregator);

        var result = await handler.HandleAsync(
            new GetFunnelReportQuery(NowUtc, NowUtc.AddDays(-1)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AnalyticsErrors.ValidationError, result.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetRetentionCohortsHandler_InvalidCohortCount_ReturnsFailure()
    {
        var calculator = new RetentionCalculator(new InMemoryActivationEventRepository());
        var handler = new GetRetentionCohortsHandler(calculator, new StubTimeProvider(NowUtc));

        var result = await handler.HandleAsync(
            new GetRetentionCohortsQuery(0),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AnalyticsErrors.ValidationError, result.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetRevenueMetricsHandler_Success()
    {
        var calculator = new RevenueCalculator(new InMemorySubscriptionRepository());
        var handler = new GetRevenueMetricsHandler(calculator, new StubTimeProvider(NowUtc));

        var result = await handler.HandleAsync(
            new GetRevenueMetricsQuery(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Metrics);
    }

    #endregion

    #region WeeklyReport Domain Tests

    [Fact]
    [Trait("Category", "Unit")]
    public void WeeklyReport_Create_SetsPropertiesCorrectly()
    {
        var periodStart = NowUtc.AddDays(-7);
        var periodEnd = NowUtc;

        var report = WeeklyReport.Create(periodStart, periodEnd, "weekly_summary", "{}", NowUtc);

        Assert.NotEqual(Guid.Empty, report.Id);
        Assert.Equal(periodStart, report.PeriodStart);
        Assert.Equal(periodEnd, report.PeriodEnd);
        Assert.Equal("weekly_summary", report.ReportType);
        Assert.Equal("{}", report.Data);
        Assert.Equal(NowUtc, report.GeneratedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void WeeklyReport_Create_EmptyReportType_Throws()
    {
        Assert.Throws<AnalyticsDomainException>(() =>
            WeeklyReport.Create(NowUtc.AddDays(-7), NowUtc, "", "{}", NowUtc));
    }

    #endregion

    #region InMemoryWeeklyReportRepository Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InMemoryWeeklyReportRepository_AddAndGetByType()
    {
        var repository = new InMemoryWeeklyReportRepository();
        var report = WeeklyReport.Create(NowUtc.AddDays(-7), NowUtc, "weekly_summary", "{}", NowUtc);

        await repository.AddAsync(report, CancellationToken.None);

        var results = await repository.GetByTypeAsync("weekly_summary", 10, CancellationToken.None);
        Assert.Single(results);
        Assert.Equal(report.Id, results[0].Id);
    }

    #endregion

    #region Helpers

    private static ActivationEvent CreateEvent(
        Guid userId,
        ActivationMilestone milestone,
        DateTimeOffset occurredAtUtc)
    {
        return ActivationEvent.Create(
            userId, milestone, householdId: null, "backend", region: null,
            metadata: null, occurredAtUtc, occurredAtUtc);
    }

    private static async Task SeedEvent(
        InMemoryActivationEventRepository repository,
        Guid userId,
        ActivationMilestone milestone,
        DateTimeOffset occurredAtUtc)
    {
        var evt = ActivationEvent.Create(
            userId, milestone, householdId: null, "backend", region: null,
            metadata: null, occurredAtUtc, occurredAtUtc);
        await repository.AddAsync(evt, CancellationToken.None);
    }

    #endregion
}
