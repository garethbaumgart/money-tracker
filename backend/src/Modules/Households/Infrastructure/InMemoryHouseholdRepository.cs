using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Modules.Households.Infrastructure;

public sealed class InMemoryHouseholdRepository : IHouseholdRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<string, Household> _householdsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<HouseholdId, Household> _householdsById = new();
    private readonly Dictionary<string, HouseholdInvitation> _invitationsByToken = new();

    public Task<bool> AddIfNotExistsAsync(Household household, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<bool>(cancellationToken);
        }

        lock (_sync)
        {
            if (_householdsByName.ContainsKey(household.Name))
            {
                return Task.FromResult(false);
            }

            _householdsByName[household.Name] = household;
            _householdsById[household.Id] = household;
            return Task.FromResult(true);
        }
    }

    public Task<Household?> GetByIdAsync(HouseholdId householdId, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<Household?>(cancellationToken);
        }

        lock (_sync)
        {
            return Task.FromResult<Household?>(_householdsById.GetValueOrDefault(householdId));
        }
    }

    public Task<bool> IsMemberAsync(HouseholdId householdId, Guid userId, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<bool>(cancellationToken);
        }

        lock (_sync)
        {
            return Task.FromResult(_householdsById.TryGetValue(householdId, out var household)
                && household.IsMember(userId));
        }
    }

    public Task<bool> AddMemberAsync(HouseholdId householdId, Guid userId, string role, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<bool>(cancellationToken);
        }

        lock (_sync)
        {
            if (!_householdsById.TryGetValue(householdId, out var household))
            {
                return Task.FromResult(false);
            }

            var added = household.TryAddMember(userId, role);
            return Task.FromResult(added);
        }
    }

    public Task<bool> AddInvitationAsync(HouseholdInvitation invitation, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<bool>(cancellationToken);
        }

        lock (_sync)
        {
            if (_invitationsByToken.ContainsKey(invitation.Token))
            {
                return Task.FromResult(false);
            }

            _invitationsByToken[invitation.Token] = invitation;
            return Task.FromResult(true);
        }
    }

    public Task<HouseholdInvitation?> GetInvitationAsync(
        string invitationToken,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<HouseholdInvitation?>(cancellationToken);
        }

        lock (_sync)
        {
            return Task.FromResult<HouseholdInvitation?>(_invitationsByToken.GetValueOrDefault(invitationToken));
        }
    }

    public Task<bool> MarkInvitationUsedAsync(
        string invitationToken,
        Guid acceptingUserId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<bool>(cancellationToken);
        }

        lock (_sync)
        {
            if (!_invitationsByToken.TryGetValue(invitationToken, out var invitation) || invitation.IsUsed)
            {
                return Task.FromResult(false);
            }

            invitation.MarkUsed(acceptingUserId);
            return Task.FromResult(true);
        }
    }
}
