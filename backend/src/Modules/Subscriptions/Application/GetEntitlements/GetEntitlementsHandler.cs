using MoneyTracker.Modules.SharedKernel.Households;
using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Subscriptions.Application.GetEntitlements;

public sealed class GetEntitlementsHandler(
    ISubscriptionEntitlementService entitlementService,
    IHouseholdAccessService householdAccess)
{
    public async Task<GetEntitlementsResult> HandleAsync(
        GetEntitlementsQuery query,
        CancellationToken cancellationToken)
    {
        var accessResult = await householdAccess.CheckMemberAsync(
            query.HouseholdId,
            query.UserId,
            cancellationToken);

        if (!accessResult.HouseholdExists)
        {
            return GetEntitlementsResult.HouseholdNotFound();
        }

        if (!accessResult.IsMember)
        {
            return GetEntitlementsResult.AccessDenied();
        }

        var entitlement = await entitlementService.GetEntitlementsAsync(
            query.HouseholdId,
            cancellationToken);

        return GetEntitlementsResult.Success(entitlement);
    }
}
