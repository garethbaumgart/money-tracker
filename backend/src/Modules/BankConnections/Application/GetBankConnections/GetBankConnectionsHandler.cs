using MoneyTracker.Modules.BankConnections.Domain;
using MoneyTracker.Modules.SharedKernel.Households;

namespace MoneyTracker.Modules.BankConnections.Application.GetBankConnections;

public sealed class GetBankConnectionsHandler(
    IBankConnectionRepository repository,
    IHouseholdAccessService householdAccessService)
{
    public async Task<GetBankConnectionsResult> HandleAsync(
        GetBankConnectionsQuery query,
        CancellationToken cancellationToken)
    {
        var access = await householdAccessService.CheckMemberAsync(
            query.HouseholdId,
            query.RequestingUserId,
            cancellationToken);
        if (!access.HouseholdExists)
        {
            return GetBankConnectionsResult.HouseholdNotFound();
        }

        if (!access.IsMember)
        {
            return GetBankConnectionsResult.AccessDenied();
        }

        var connections = await repository.GetByHouseholdAsync(
            query.HouseholdId,
            cancellationToken);

        var summaries = connections
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new BankConnectionSummary(
                c.Id.Value,
                c.HouseholdId,
                c.InstitutionName,
                c.Status.ToString(),
                c.ErrorCode,
                c.ErrorMessage,
                c.CreatedAtUtc,
                c.UpdatedAtUtc))
            .ToArray();

        return GetBankConnectionsResult.Success(summaries);
    }
}
