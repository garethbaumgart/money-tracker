namespace MoneyTracker.Modules.BankConnections.Domain;

public interface ILinkEventRepository
{
    Task AddAsync(LinkEvent linkEvent, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LinkEvent>> GetByPeriodAsync(DateTimeOffset since, CancellationToken cancellationToken);
}
