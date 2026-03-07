using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Application.ProcessCallback;

public sealed class ProcessCallbackHandler(
    IBankConnectionRepository repository,
    IBankProviderAdapter providerAdapter,
    TimeProvider timeProvider)
{
    public async Task<ProcessCallbackResult> HandleAsync(
        ProcessCallbackCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.ConsentSessionId))
        {
            return ProcessCallbackResult.Validation(
                BankConnectionErrors.ConnectionCallbackInvalid,
                "Consent session ID is required.");
        }

        var connection = await repository.GetByConsentSessionIdAsync(
            command.ConsentSessionId,
            cancellationToken);
        if (connection is null)
        {
            return ProcessCallbackResult.ConnectionNotFound();
        }

        var connectionResult = await providerAdapter.GetConnectionAsync(
            connection.ExternalUserId,
            command.ConsentSessionId,
            cancellationToken);

        var nowUtc = timeProvider.GetUtcNow();

        if (!connectionResult.IsSuccess)
        {
            try
            {
                connection.MarkFailed(
                    connectionResult.ErrorCode ?? BankConnectionErrors.ConnectionProviderError,
                    connectionResult.ErrorMessage ?? "Provider returned an error.",
                    nowUtc);
                await repository.UpdateAsync(connection, cancellationToken);
            }
            catch (BankConnectionDomainException)
            {
                // Connection may already be in a terminal state; persist as-is.
            }

            return ProcessCallbackResult.Failed(
                connection,
                connectionResult.ErrorCode ?? BankConnectionErrors.ConnectionProviderError,
                connectionResult.ErrorMessage ?? "Provider returned an error.");
        }

        try
        {
            if (connection.Status is BankConnectionStatus.Expired or BankConnectionStatus.Revoked)
            {
                // Re-consent flow: reactivate from Expired/Revoked
                var consentExpiry = nowUtc.AddDays(90); // Default 90-day consent
                connection.ReactivateAfterReConsent(
                    connectionResult.ConnectionId!,
                    connectionResult.InstitutionName,
                    consentExpiry,
                    nowUtc);
            }
            else
            {
                // Initial consent flow: activate from Pending
                connection.Activate(
                    connectionResult.ConnectionId!,
                    connectionResult.InstitutionName,
                    nowUtc);
                // Set initial consent expiry
                connection.UpdateConsentExpiry(nowUtc.AddDays(90), nowUtc);
            }
        }
        catch (BankConnectionDomainException exception)
        {
            return ProcessCallbackResult.Validation(exception.Code, exception.Message);
        }

        await repository.UpdateAsync(connection, cancellationToken);
        return ProcessCallbackResult.Success(connection);
    }
}
