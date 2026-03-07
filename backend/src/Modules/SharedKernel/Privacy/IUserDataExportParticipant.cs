namespace MoneyTracker.Modules.SharedKernel.Privacy;

public interface IUserDataExportParticipant
{
    Task<object> ExportUserDataAsync(Guid userId, CancellationToken ct);
}
