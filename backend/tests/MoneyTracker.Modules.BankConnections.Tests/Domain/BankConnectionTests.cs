using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Tests.Domain;

public sealed class BankConnectionTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T00:00:00Z");

    [Fact]
    [Trait("Category", "Unit")]
    public void Activate_FromPending_Succeeds()
    {
        // P3-1-UNIT-01: Valid state transition Pending -> Active succeeds
        var connection = BankConnection.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ext-user-1",
            "session-1",
            NowUtc);

        connection.Activate("conn-123", "Test Bank", NowUtc.AddMinutes(5));

        Assert.Equal(BankConnectionStatus.Active, connection.Status);
        Assert.Equal("conn-123", connection.ExternalConnectionId);
        Assert.Equal("Test Bank", connection.InstitutionName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Activate_FromFailed_ThrowsDomainException()
    {
        // P3-1-UNIT-02: Invalid state transition Failed -> Active throws domain exception
        var connection = BankConnection.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ext-user-1",
            "session-1",
            NowUtc);

        connection.MarkFailed("some_error", "Some error", NowUtc.AddMinutes(1));

        var exception = Assert.Throws<BankConnectionDomainException>(
            () => connection.Activate("conn-123", "Test Bank", NowUtc.AddMinutes(5)));

        Assert.Equal(BankConnectionErrors.ConnectionInvalidStateTransition, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void MarkFailed_FromPending_Succeeds()
    {
        var connection = BankConnection.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ext-user-1",
            "session-1",
            NowUtc);

        connection.MarkFailed("provider_error", "Something went wrong", NowUtc.AddMinutes(1));

        Assert.Equal(BankConnectionStatus.Failed, connection.Status);
        Assert.Equal("provider_error", connection.ErrorCode);
        Assert.Equal("Something went wrong", connection.ErrorMessage);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Revoke_FromActive_Succeeds()
    {
        var connection = BankConnection.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ext-user-1",
            "session-1",
            NowUtc);
        connection.Activate("conn-123", "Test Bank", NowUtc.AddMinutes(1));

        connection.Revoke(NowUtc.AddMinutes(5));

        Assert.Equal(BankConnectionStatus.Revoked, connection.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Revoke_FromPending_ThrowsDomainException()
    {
        var connection = BankConnection.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ext-user-1",
            "session-1",
            NowUtc);

        var exception = Assert.Throws<BankConnectionDomainException>(
            () => connection.Revoke(NowUtc.AddMinutes(5)));

        Assert.Equal(BankConnectionErrors.ConnectionInvalidStateTransition, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Activate_FromActive_ThrowsDomainException()
    {
        var connection = BankConnection.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ext-user-1",
            "session-1",
            NowUtc);
        connection.Activate("conn-123", "Test Bank", NowUtc.AddMinutes(1));

        var exception = Assert.Throws<BankConnectionDomainException>(
            () => connection.Activate("conn-456", "Other Bank", NowUtc.AddMinutes(5)));

        Assert.Equal(BankConnectionErrors.ConnectionInvalidStateTransition, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CreatePending_WithEmptyExternalUserId_ThrowsDomainException()
    {
        var exception = Assert.Throws<BankConnectionDomainException>(
            () => BankConnection.CreatePending(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "",
                "session-1",
                NowUtc));

        Assert.Equal(BankConnectionErrors.ValidationError, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CreatePending_WithEmptyConsentSessionId_ThrowsDomainException()
    {
        var exception = Assert.Throws<BankConnectionDomainException>(
            () => BankConnection.CreatePending(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "ext-user-1",
                "",
                NowUtc));

        Assert.Equal(BankConnectionErrors.ValidationError, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CreatePending_SetsStatusToPending()
    {
        var connection = BankConnection.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ext-user-1",
            "session-1",
            NowUtc);

        Assert.Equal(BankConnectionStatus.Pending, connection.Status);
        Assert.Equal(NowUtc, connection.CreatedAtUtc);
    }
}
