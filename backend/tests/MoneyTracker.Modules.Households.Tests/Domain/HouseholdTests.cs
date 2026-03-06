using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Modules.Households.Tests.Domain;

public sealed class HouseholdTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Create_TrimsNameAndSetsStableFields()
    {
        var ownerUserId = Guid.NewGuid();
        var now = DateTimeOffset.Parse("2026-01-02T03:04:05Z");

        var household = Household.Create("  Family Budget  ", ownerUserId, now);

        Assert.Equal("Family Budget", household.Name);
        Assert.Equal(now, household.CreatedAtUtc);
        Assert.NotEqual(Guid.Empty, household.Id.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Create_ThrowsValidationError_WhenNameIsBlank()
    {
        var exception = Assert.Throws<HouseholdDomainException>(() => Household.Create("   ", Guid.NewGuid(), DateTimeOffset.UtcNow));

        Assert.Equal(HouseholdErrors.ValidationError, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Create_ThrowsValidationError_WhenNameExceedsMaxLength()
    {
        var longName = new string('a', Household.MaxNameLength + 1);

        var exception = Assert.Throws<HouseholdDomainException>(() => Household.Create(longName, Guid.NewGuid(), DateTimeOffset.UtcNow));

        Assert.Equal(HouseholdErrors.ValidationError, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Create_AcceptsNameAtMaxLengthBoundary()
    {
        var boundaryLengthName = new string('a', Household.MaxNameLength);

        var household = Household.Create(boundaryLengthName, Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(boundaryLengthName, household.Name);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void NormalizeName_ReturnsEmpty_ForNull()
    {
        var normalized = Household.NormalizeName(null);

        Assert.Equal(string.Empty, normalized);
    }
}
