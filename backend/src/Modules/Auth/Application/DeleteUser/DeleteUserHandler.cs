using MoneyTracker.Modules.Auth.Domain;

namespace MoneyTracker.Modules.Auth.Application.DeleteUser;

public sealed class DeleteUserHandler(
    IAuthRepository authRepository,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan PurgeDelay = TimeSpan.FromDays(30);

    public async Task<DeleteUserResult> HandleAsync(
        DeleteUserCommand command,
        IReadOnlyList<Func<Guid, CancellationToken, Task>> deletionParticipants,
        CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow();
        var scheduledPurgeAtUtc = nowUtc.Add(PurgeDelay);

        await authRepository.MarkUserDeletedAsync(command.UserId, scheduledPurgeAtUtc, cancellationToken);

        foreach (var deleteAsync in deletionParticipants)
        {
            await deleteAsync(command.UserId, cancellationToken);
        }

        return DeleteUserResult.Success(scheduledPurgeAtUtc);
    }
}
