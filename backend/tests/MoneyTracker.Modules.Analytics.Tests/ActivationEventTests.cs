using MoneyTracker.Modules.Analytics.Domain;

namespace MoneyTracker.Modules.Analytics.Tests;

public sealed class ActivationEventTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Create_WithValidFields_ReturnsEvent()
    {
        // P5-2-UNIT-07: ActivationEvent.Create() validates required fields
        var userId = Guid.NewGuid();
        var occurredAtUtc = DateTimeOffset.Parse("2026-03-01T12:00:00Z");
        var recordedAtUtc = DateTimeOffset.Parse("2026-03-01T12:00:01Z");

        var evt = ActivationEvent.Create(
            userId,
            ActivationMilestone.SignupCompleted,
            householdId: null,
            "ios",
            "AU",
            metadata: null,
            occurredAtUtc,
            recordedAtUtc);

        Assert.Equal(userId, evt.UserId);
        Assert.Equal(ActivationMilestone.SignupCompleted, evt.Milestone);
        Assert.Equal("ios", evt.Platform);
        Assert.Equal("AU", evt.Region);
        Assert.Equal(occurredAtUtc, evt.OccurredAtUtc);
        Assert.Equal(recordedAtUtc, evt.RecordedAtUtc);
        Assert.NotEqual(Guid.Empty, evt.Id);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Create_WithEmptyUserId_ThrowsAnalyticsDomainException()
    {
        // P5-2-UNIT-07: Throws AnalyticsDomainException on empty userId
        var exception = Assert.Throws<AnalyticsDomainException>(() =>
            ActivationEvent.Create(
                Guid.Empty,
                ActivationMilestone.SignupCompleted,
                householdId: null,
                "ios",
                "AU",
                metadata: null,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow));

        Assert.Equal(AnalyticsErrors.ValidationError, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Create_WithEmptyPlatform_ThrowsAnalyticsDomainException()
    {
        // P5-2-UNIT-07: Throws AnalyticsDomainException on null platform
        var exception = Assert.Throws<AnalyticsDomainException>(() =>
            ActivationEvent.Create(
                Guid.NewGuid(),
                ActivationMilestone.SignupCompleted,
                householdId: null,
                "  ",
                "AU",
                metadata: null,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow));

        Assert.Equal(AnalyticsErrors.ValidationError, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ActivationMilestone_TryParse_ValidNames()
    {
        Assert.True(ActivationMilestoneExtensions.TryParse("signup_completed", out var m1));
        Assert.Equal(ActivationMilestone.SignupCompleted, m1);

        Assert.True(ActivationMilestoneExtensions.TryParse("household_created", out var m2));
        Assert.Equal(ActivationMilestone.HouseholdCreated, m2);

        Assert.True(ActivationMilestoneExtensions.TryParse("paid_conversion", out var m3));
        Assert.Equal(ActivationMilestone.PaidConversion, m3);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ActivationMilestone_TryParse_InvalidName_ReturnsFalse()
    {
        Assert.False(ActivationMilestoneExtensions.TryParse("invalid_milestone", out _));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ActivationMilestone_ToSnakeCase_ReturnsCorrectNames()
    {
        Assert.Equal("signup_completed", ActivationMilestone.SignupCompleted.ToSnakeCase());
        Assert.Equal("household_created", ActivationMilestone.HouseholdCreated.ToSnakeCase());
        Assert.Equal("paid_conversion", ActivationMilestone.PaidConversion.ToSnakeCase());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void OrderedStages_Contains12Milestones()
    {
        Assert.Equal(12, ActivationMilestoneExtensions.OrderedStages.Count);
        Assert.Equal(ActivationMilestone.SignupCompleted, ActivationMilestoneExtensions.OrderedStages[0]);
        Assert.Equal(ActivationMilestone.PaidConversion, ActivationMilestoneExtensions.OrderedStages[^1]);
    }
}
