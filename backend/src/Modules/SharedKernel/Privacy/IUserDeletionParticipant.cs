namespace MoneyTracker.Modules.SharedKernel.Privacy;

public interface IUserDeletionParticipant
{
    Task DeleteUserDataAsync(Guid userId, CancellationToken ct);
}
