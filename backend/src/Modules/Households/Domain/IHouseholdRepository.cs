namespace MoneyTracker.Modules.Households.Domain;

public interface IHouseholdRepository
{
    Task<bool> AddIfNotExistsAsync(Household household, CancellationToken cancellationToken);
    Task<Household?> GetByIdAsync(HouseholdId householdId, CancellationToken cancellationToken);
    Task<bool> IsMemberAsync(HouseholdId householdId, Guid userId, CancellationToken cancellationToken);
    Task<bool> AddMemberAsync(HouseholdId householdId, Guid userId, string role, CancellationToken cancellationToken);

    Task<bool> AddInvitationAsync(HouseholdInvitation invitation, CancellationToken cancellationToken);
    Task<HouseholdInvitation?> GetInvitationAsync(string invitationToken, CancellationToken cancellationToken);
    Task<bool> MarkInvitationUsedAsync(
        string invitationToken,
        Guid acceptingUserId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);
}
