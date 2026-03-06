using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Modules.Households.Infrastructure;

public sealed class InMemoryHouseholdRepository : IHouseholdRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<string, Household> _households = new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            return Task.FromResult(_households.ContainsKey(name));
        }
    }

    public Task AddAsync(Household household, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            _households[household.Name] = household;
        }

        return Task.CompletedTask;
    }
}
