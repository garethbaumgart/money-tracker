using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Tests.Domain;

public sealed class PilotMetricEventTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T12:00:00Z");

    [Fact]
    [Trait("Category", "Unit")]
    public void SyncEvent_Created_AllFieldsPopulated()
    {
        // P3-4-UNIT-01: SyncEvent created with all required fields -> no nulls
        var connectionId = BankConnectionId.New();

        var syncEvent = SyncEvent.Create(
            connectionId,
            "CBA",
            "AU",
            EventOutcome.Success,
            durationMs: 1200,
            transactionCount: 15,
            errorCategory: null,
            NowUtc);

        Assert.NotEqual(Guid.Empty, syncEvent.Id.Value);
        Assert.Equal(connectionId, syncEvent.ConnectionId);
        Assert.Equal("CBA", syncEvent.Institution);
        Assert.Equal("AU", syncEvent.Region);
        Assert.Equal(EventOutcome.Success, syncEvent.Outcome);
        Assert.Equal(1200, syncEvent.DurationMs);
        Assert.Equal(15, syncEvent.TransactionCount);
        Assert.Null(syncEvent.ErrorCategory);
        Assert.Equal(NowUtc, syncEvent.OccurredAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void LinkEvent_WithFailure_ErrorCategoryPopulated()
    {
        // P3-4-UNIT-02: LinkEvent with failure outcome -> ErrorCategory populated, Outcome=Failed
        var linkEvent = LinkEvent.Create(
            "ANZ NZ",
            "NZ",
            EventOutcome.Failed,
            durationMs: 5000,
            errorCategory: "ProviderTimeout",
            NowUtc);

        Assert.NotEqual(Guid.Empty, linkEvent.Id.Value);
        Assert.Equal("ANZ NZ", linkEvent.Institution);
        Assert.Equal("NZ", linkEvent.Region);
        Assert.Equal(EventOutcome.Failed, linkEvent.Outcome);
        Assert.Equal(5000, linkEvent.DurationMs);
        Assert.Equal("ProviderTimeout", linkEvent.ErrorCategory);
        Assert.Equal(NowUtc, linkEvent.OccurredAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void SyncEvent_FailedWithoutErrorCategory_ThrowsDomainException()
    {
        var exception = Assert.Throws<BankConnectionDomainException>(
            () => SyncEvent.Create(
                BankConnectionId.New(),
                "CBA",
                "AU",
                EventOutcome.Failed,
                durationMs: 1000,
                transactionCount: 0,
                errorCategory: null,
                NowUtc));

        Assert.Equal(PilotMetricErrors.ValidationError, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void LinkEvent_FailedWithoutErrorCategory_ThrowsDomainException()
    {
        var exception = Assert.Throws<BankConnectionDomainException>(
            () => LinkEvent.Create(
                "CBA",
                "AU",
                EventOutcome.Failed,
                durationMs: 1000,
                errorCategory: null,
                NowUtc));

        Assert.Equal(PilotMetricErrors.ValidationError, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void SyncEvent_WithEmptyInstitution_ThrowsDomainException()
    {
        var exception = Assert.Throws<BankConnectionDomainException>(
            () => SyncEvent.Create(
                BankConnectionId.New(),
                "",
                "AU",
                EventOutcome.Success,
                durationMs: 1000,
                transactionCount: 5,
                errorCategory: null,
                NowUtc));

        Assert.Equal(PilotMetricErrors.ValidationError, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void SyncEvent_WithNegativeDuration_ThrowsDomainException()
    {
        var exception = Assert.Throws<BankConnectionDomainException>(
            () => SyncEvent.Create(
                BankConnectionId.New(),
                "CBA",
                "AU",
                EventOutcome.Success,
                durationMs: -1,
                transactionCount: 0,
                errorCategory: null,
                NowUtc));

        Assert.Equal(PilotMetricErrors.ValidationError, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void SyncEvent_RegionIsNormalized_ToUpperCase()
    {
        var syncEvent = SyncEvent.Create(
            BankConnectionId.New(),
            "CBA",
            "au",
            EventOutcome.Success,
            durationMs: 500,
            transactionCount: 3,
            errorCategory: null,
            NowUtc);

        Assert.Equal("AU", syncEvent.Region);
    }
}
