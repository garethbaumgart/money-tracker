using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MoneyTracker.Modules.BankConnections.Domain;
using MoneyTracker.Modules.SharedKernel.Analytics;
using MoneyTracker.Modules.SharedKernel.Households;

namespace MoneyTracker.Modules.BankConnections.Application.CreateLinkSession;

public sealed class CreateLinkSessionHandler(
    IBankConnectionRepository repository,
    IBankProviderAdapter providerAdapter,
    IHouseholdAccessService householdAccessService,
    ILinkEventRepository linkEventRepository,
    IAnalyticsEventPublisher analyticsPublisher,
    TimeProvider timeProvider,
    ILogger<CreateLinkSessionHandler> logger)
{
    private const string DefaultRegion = "AU";
    private const string DefaultInstitution = "Unknown";

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

        var stopwatch = Stopwatch.StartNew();

        var createUserResult = await providerAdapter.CreateUserAsync(
            command.HouseholdId,
            command.RequestingUserId,
            cancellationToken);
        if (!createUserResult.IsSuccess)
        {
            stopwatch.Stop();
            await RecordLinkEventAsync(
                DefaultInstitution,
                EventOutcome.Failed,
                stopwatch.ElapsedMilliseconds,
                createUserResult.ErrorCode,
                cancellationToken);

            return CreateLinkSessionResult.ProviderError(
                createUserResult.ErrorCode,
                createUserResult.ErrorMessage);
        }

        var consentResult = await providerAdapter.CreateConsentSessionAsync(
            createUserResult.ExternalUserId!,
            cancellationToken);
        if (!consentResult.IsSuccess)
        {
            stopwatch.Stop();
            await RecordLinkEventAsync(
                DefaultInstitution,
                EventOutcome.Failed,
                stopwatch.ElapsedMilliseconds,
                consentResult.ErrorCode,
                cancellationToken);

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
            stopwatch.Stop();
            await RecordLinkEventAsync(
                DefaultInstitution,
                EventOutcome.Failed,
                stopwatch.ElapsedMilliseconds,
                exception.Code,
                cancellationToken);

            return CreateLinkSessionResult.Validation(exception.Code, exception.Message);
        }

        await repository.AddAsync(connection, cancellationToken);
        stopwatch.Stop();

        await RecordLinkEventAsync(
            DefaultInstitution,
            EventOutcome.Success,
            stopwatch.ElapsedMilliseconds,
            errorCategory: null,
            cancellationToken);

        await analyticsPublisher.PublishAsync(
            command.RequestingUserId, "bank_link_started", command.HouseholdId, cancellationToken);

        return CreateLinkSessionResult.Success(consentResult.ConsentUrl!, connection.Id.Value);
    }

    private async Task RecordLinkEventAsync(
        string institution,
        EventOutcome outcome,
        long durationMs,
        string? errorCategory,
        CancellationToken cancellationToken)
    {
        try
        {
            var linkEvent = LinkEvent.Create(
                institution,
                DefaultRegion,
                outcome,
                durationMs,
                errorCategory,
                timeProvider.GetUtcNow());

            await linkEventRepository.AddAsync(linkEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to record link event.");
        }
    }
}
