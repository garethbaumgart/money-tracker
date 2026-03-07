using MoneyTracker.Modules.BankConnections.Domain;
using MoneyTracker.Modules.SharedKernel.Households;

namespace MoneyTracker.Modules.BankConnections.Application.CreateLinkSession;

public sealed class CreateLinkSessionHandler(
    IBankConnectionRepository repository,
    IBankProviderAdapter providerAdapter,
    IHouseholdAccessService householdAccessService,
    TimeProvider timeProvider)
{
    public async Task<CreateLinkSessionResult> HandleAsync(
        CreateLinkSessionCommand command,
        CancellationToken cancellationToken)
    {
        var access = await householdAccessService.CheckMemberAsync(
            command.HouseholdId,
            command.RequestingUserId,
            cancellationToken);
        if (!access.HouseholdExists)
        {
            return CreateLinkSessionResult.HouseholdNotFound();
        }

        if (!access.IsMember)
        {
            return CreateLinkSessionResult.AccessDenied();
        }

        var createUserResult = await providerAdapter.CreateUserAsync(
            command.HouseholdId,
            command.RequestingUserId,
            cancellationToken);
        if (!createUserResult.IsSuccess)
        {
            return CreateLinkSessionResult.ProviderError(
                createUserResult.ErrorCode,
                createUserResult.ErrorMessage);
        }

        var consentResult = await providerAdapter.CreateConsentSessionAsync(
            createUserResult.ExternalUserId!,
            cancellationToken);
        if (!consentResult.IsSuccess)
        {
            return CreateLinkSessionResult.ProviderError(
                consentResult.ErrorCode,
                consentResult.ErrorMessage);
        }

        BankConnection connection;
        try
        {
            connection = BankConnection.CreatePending(
                command.HouseholdId,
                command.RequestingUserId,
                createUserResult.ExternalUserId!,
                consentResult.SessionId!,
                timeProvider.GetUtcNow());
        }
        catch (BankConnectionDomainException exception)
        {
            return CreateLinkSessionResult.Validation(exception.Code, exception.Message);
        }

        await repository.AddAsync(connection, cancellationToken);
        return CreateLinkSessionResult.Success(consentResult.ConsentUrl!, connection.Id.Value);
    }
}
