namespace MoneyTracker.Modules.BankConnections.Domain;

public interface ISyncEventRepository
{
    Task AddAsync(SyncEvent syncEvent, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<SyncEvent>> GetByPeriodAsync(DateTimeOffset since, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<SyncEvent>> GetByRegionAsync(string region, DateTimeOffset since, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<SyncEvent>> GetByInstitutionAsync(string institution, DateTimeOffset since, CancellationToken cancellationToken);
}
