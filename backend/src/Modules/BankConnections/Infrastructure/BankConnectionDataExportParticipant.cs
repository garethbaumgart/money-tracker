using MoneyTracker.Modules.BankConnections.Domain;
using MoneyTracker.Modules.SharedKernel.Privacy;

namespace MoneyTracker.Modules.BankConnections.Infrastructure;

public sealed class BankConnectionDataExportParticipant(IBankConnectionRepository repository) : IUserDataExportParticipant, IUserDeletionParticipant
{
    public async Task<object> ExportUserDataAsync(Guid userId, CancellationToken ct)
    {
        var allConnections = await repository.GetAllConnectionsAsync(ct);
        var userConnections = allConnections
            .Where(c => c.CreatedByUserId == userId)
            .Select(c => new
            {
                c.Id,
                c.HouseholdId,
                c.InstitutionName,
                Status = c.Status.ToString(),
                ConsentStatus = c.ConsentStatus.ToString(),
                c.ConsentExpiresAtUtc,
                c.CreatedAtUtc,
                c.UpdatedAtUtc
                // Excludes ExternalConnectionId and ConsentSessionId for security
            })
            .ToArray();

        return new { connections = userConnections };
    }

    public Task DeleteUserDataAsync(Guid userId, CancellationToken ct)
    {
        // Bank connections created by user will be revoked during purge phase.
        return Task.CompletedTask;
    }
}
