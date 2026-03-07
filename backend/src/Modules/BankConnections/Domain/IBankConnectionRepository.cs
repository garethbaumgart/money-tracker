namespace MoneyTracker.Modules.BankConnections.Domain;

public interface IBankConnectionRepository
{
    Task AddAsync(BankConnection connection, CancellationToken cancellationToken);
    Task UpdateAsync(BankConnection connection, CancellationToken cancellationToken);
    Task<BankConnection?> GetByIdAsync(BankConnectionId id, CancellationToken cancellationToken);
    Task<BankConnection?> GetByConsentSessionIdAsync(string consentSessionId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<BankConnection>> GetByHouseholdAsync(
        Guid householdId,
        CancellationToken cancellationToken);
    Task<IReadOnlyCollection<BankConnection>> GetActiveConnectionsAsync(
        CancellationToken cancellationToken);
    Task<IReadOnlyCollection<BankConnection>> GetAllConnectionsAsync(
        CancellationToken cancellationToken);
}
