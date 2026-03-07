using MoneyTracker.Modules.BankConnections.Domain;
using MoneyTracker.Modules.SharedKernel.Households;

namespace MoneyTracker.Modules.BankConnections.Application.ReConsent;

public sealed class ReConsentHandler(
    IBankConnectionRepository connectionRepository,
    IBankProviderAdapter providerAdapter,
    IHouseholdAccessService householdAccessService,
    TimeProvider timeProvider)
{
    public async Task<ReConsentResult> HandleAsync(
        ReConsentCommand command,
        CancellationToken cancellationToken)
    {
        var connection = await connectionRepository.GetByIdAsync(
            command.ConnectionId,
            cancellationToken);

        if (connection is null)
        {
            return ReConsentResult.ConnectionNotFound();
        }

        var access = await householdAccessService.CheckMemberAsync(
            connection.HouseholdId,
            command.RequestingUserId,
            cancellationToken);

        if (!access.HouseholdExists)
        {
            return ReConsentResult.ConnectionNotFound();
        }

        if (!access.IsMember)
        {
            return ReConsentResult.AccessDenied();
        }

        if (connection.Status is not (BankConnectionStatus.Expired or BankConnectionStatus.Revoked))
        {
            return ReConsentResult.ReConsentNotNeeded();
        }

        var consentResult = await providerAdapter.CreateConsentSessionAsync(
            connection.ExternalUserId,
            cancellationToken);

        if (!consentResult.IsSuccess)
        {
            return ReConsentResult.ProviderError(
                consentResult.ErrorCode,
                consentResult.ErrorMessage);
        }

        // Update the connection's consent session so the callback can find it
        var nowUtc = timeProvider.GetUtcNow();
        connection.UpdateConsentSessionId(consentResult.SessionId!, nowUtc);
        await connectionRepository.UpdateAsync(connection, cancellationToken);

        return ReConsentResult.Success(consentResult.ConsentUrl!, consentResult.SessionId!);
    }
}
