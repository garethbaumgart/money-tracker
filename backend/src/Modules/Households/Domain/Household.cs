namespace MoneyTracker.Modules.Households.Domain;

public sealed class Household
{
    public const int MaxNameLength = 100;

    public HouseholdId Id { get; }
    public string Name { get; }
    public Guid OwnerUserId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    private readonly List<HouseholdMember> _members;

    private Household(
        HouseholdId id,
        string name,
        DateTimeOffset createdAtUtc,
        Guid ownerUserId,
        IEnumerable<HouseholdMember>? initialMembers = null)
    {
        Id = id;
        Name = name;
        OwnerUserId = ownerUserId;
        CreatedAtUtc = createdAtUtc;
        _members = initialMembers?.ToList() ?? [];
    }

    public static Household Create(string name, Guid ownerUserId, DateTimeOffset nowUtc)
    {
        var normalizedName = NormalizeName(name);
        if (normalizedName.Length == 0)
        {
            throw new HouseholdDomainException(
                HouseholdErrors.ValidationError,
                "Household name is required.");
        }

        if (normalizedName.Length > MaxNameLength)
        {
            throw new HouseholdDomainException(
                HouseholdErrors.ValidationError,
                $"Household name must be {MaxNameLength} characters or fewer.");
        }

        return new Household(
            HouseholdId.New(),
            normalizedName,
            nowUtc,
            ownerUserId,
            new[] { new HouseholdMember(ownerUserId, HouseholdRole.Owner) });
    }

    public IReadOnlyCollection<HouseholdMember> Members => _members.AsReadOnly();

    public bool IsOwner(Guid userId)
    {
        return OwnerUserId == userId;
    }

    public bool IsMember(Guid userId)
    {
        return _members.Any(member => member.UserId == userId);
    }

    public bool TryAddMember(Guid userId, string role)
    {
        if (_members.Any(member => member.UserId == userId))
        {
            return false;
        }

        _members.Add(new HouseholdMember(userId, role));
        return true;
    }

    public static string NormalizeName(string? name)
    {
        return name?.Trim() ?? string.Empty;
    }
}
