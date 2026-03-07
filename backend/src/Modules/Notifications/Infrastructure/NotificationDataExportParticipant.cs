using MoneyTracker.Modules.Notifications.Domain;
using MoneyTracker.Modules.SharedKernel.Privacy;

namespace MoneyTracker.Modules.Notifications.Infrastructure;

public sealed class NotificationDataExportParticipant(INotificationTokenRepository repository) : IUserDataExportParticipant, IUserDeletionParticipant
{
    public async Task<object> ExportUserDataAsync(Guid userId, CancellationToken ct)
    {
        var tokens = await repository.GetTokensForUsersAsync([userId], ct);
        var exported = tokens.Select(t => new
        {
            t.Platform,
            t.RegisteredAtUtc
        }).ToArray();

        return new { deviceTokens = exported };
    }

    public Task DeleteUserDataAsync(Guid userId, CancellationToken ct)
    {
        // Device tokens will be removed during purge phase.
        return Task.CompletedTask;
    }
}
