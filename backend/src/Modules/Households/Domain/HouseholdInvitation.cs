namespace MoneyTracker.Modules.Households.Domain;

public sealed class HouseholdInvitation
{
    public HouseholdInvitation(
        string token,
        HouseholdId householdId,
        Guid inviterUserId,
        string inviteeEmail,
        DateTimeOffset expiresAtUtc)
    {
        Token = token;
        HouseholdId = householdId;
        InviterUserId = inviterUserId;
        InviteeEmail = inviteeEmail;
        ExpiresAtUtc = expiresAtUtc;
    }

    public string Token { get; }
    public HouseholdId HouseholdId { get; }
    public Guid InviterUserId { get; }
    public string InviteeEmail { get; }
    public DateTimeOffset ExpiresAtUtc { get; }
    public bool IsUsed { get; private set; }
    public Guid? AcceptedByUserId { get; private set; }

    public bool IsExpired(DateTimeOffset nowUtc)
    {
        return nowUtc >= ExpiresAtUtc;
    }

    public void MarkUsed(Guid acceptingUserId)
    {
        IsUsed = true;
        AcceptedByUserId = acceptingUserId;
    }

    public static string NormalizeEmail(string? email)
    {
        return email?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
