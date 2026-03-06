using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Modules.Households.Infrastructure;

public sealed class InMemoryHouseholdRepository : IHouseholdRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<string, Household> _households = new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> AddIfNotExistsAsync(Household household, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            return Task.FromResult(_households.TryAdd(household.Name, household));
        }
    }
}
