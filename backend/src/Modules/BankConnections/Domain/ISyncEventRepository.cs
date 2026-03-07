namespace MoneyTracker.Modules.BankConnections.Domain;

public interface ISyncEventRepository
{
    Task AddAsync(SyncEvent syncEvent, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<SyncEvent>> GetByPeriodAsync(DateTimeOffset since, CancellationToken cancellationToken);
}
