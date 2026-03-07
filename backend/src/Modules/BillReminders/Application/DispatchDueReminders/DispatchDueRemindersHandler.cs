using Microsoft.Extensions.Logging;
using MoneyTracker.Modules.BillReminders.Domain;
using MoneyTracker.Modules.Households.Domain;
using MoneyTracker.Modules.Notifications.Domain;

namespace MoneyTracker.Modules.BillReminders.Application.DispatchDueReminders;

public sealed class DispatchDueRemindersHandler(
    IBillReminderRepository reminderRepository,
    IHouseholdRepository householdRepository,
    INotificationTokenRepository notificationTokenRepository,
    INotificationSender notificationSender,
    TimeProvider timeProvider,
    ILogger<DispatchDueRemindersHandler> logger)
{
    public async Task<DispatchDueRemindersResult> HandleAsync(CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow();
        var dueReminders = await reminderRepository.GetDueAsync(nowUtc, cancellationToken);
        var attempted = 0;
        var sent = 0;
        var failed = 0;

        foreach (var reminder in dueReminders)
        {
            var dueDateUtc = reminder.NextDueDateUtc;
            if (await reminderRepository.HasDispatchRecordAsync(
                    reminder.Id,
                    dueDateUtc,
                    cancellationToken))
            {
                continue;
            }

            attempted += 1;

            var household = await householdRepository.GetByIdAsync(
                new HouseholdId(reminder.HouseholdId),
                cancellationToken);
            if (household is null)
            {
                failed += 1;
                logger.LogWarning(
                    "Bill reminder dispatch failed for {ReminderId} due {DueDateUtc}: household not found.",
                    reminder.Id.Value,
                    dueDateUtc);
                reminder.MarkDispatchFailure(
                    nowUtc,
                    CalculateRetryDelay(reminder.DispatchAttemptCount),
                    BillReminderErrors.ReminderDispatchFailed,
                    "Household not found for reminder dispatch.");
                await reminderRepository.UpdateAsync(reminder, cancellationToken);
                await reminderRepository.RecordDispatchAsync(
                    new BillReminderDispatchRecord(
                        reminder.Id,
                        dueDateUtc,
                        nowUtc,
                        false,
                        BillReminderErrors.ReminderDispatchFailed,
                        "Household not found for reminder dispatch."),
                    cancellationToken);
                continue;
            }

            var memberIds = household.Members.Select(member => member.UserId).ToArray();
            var tokens = await notificationTokenRepository.GetTokensForUsersAsync(memberIds, cancellationToken);
            if (tokens.Count == 0)
            {
                failed += 1;
                logger.LogWarning(
                    "Bill reminder dispatch failed for {ReminderId} due {DueDateUtc}: no device tokens.",
                    reminder.Id.Value,
                    dueDateUtc);
                reminder.MarkDispatchFailure(
                    nowUtc,
                    CalculateRetryDelay(reminder.DispatchAttemptCount),
                    BillReminderErrors.ReminderDispatchFailed,
                    "No device tokens registered for reminder dispatch.");
                await reminderRepository.UpdateAsync(reminder, cancellationToken);
                await reminderRepository.RecordDispatchAsync(
                    new BillReminderDispatchRecord(
                        reminder.Id,
                        dueDateUtc,
                        nowUtc,
                        false,
                        BillReminderErrors.ReminderDispatchFailed,
                        "No device tokens registered for reminder dispatch."),
                    cancellationToken);
                continue;
            }

            var message = new NotificationMessage(
                reminder.Id.Value,
                reminder.Title,
                reminder.Amount,
                dueDateUtc);

            var anySuccess = false;
            NotificationDispatchResult? failure = null;
            foreach (var token in tokens)
            {
                var dispatchResult = await notificationSender.SendReminderAsync(message, token, cancellationToken);
                if (dispatchResult.IsSuccess)
                {
                    anySuccess = true;
                }
                else
                {
                    failure ??= dispatchResult;
                }
            }

            if (anySuccess)
            {
                reminder.MarkDispatchSuccess(nowUtc);
                await reminderRepository.UpdateAsync(reminder, cancellationToken);
                await reminderRepository.RecordDispatchAsync(
                    new BillReminderDispatchRecord(
                        reminder.Id,
                        dueDateUtc,
                        nowUtc,
                        true,
                        ErrorCode: null,
                        ErrorMessage: null),
                    cancellationToken);
                sent += 1;
                if (failure is not null)
                {
                    logger.LogWarning(
                        "Bill reminder dispatch partially failed for {ReminderId} due {DueDateUtc}: {ErrorCode} {ErrorMessage}",
                        reminder.Id.Value,
                        dueDateUtc,
                        failure.ErrorCode ?? BillReminderErrors.ReminderDispatchFailed,
                        failure.ErrorMessage ?? "Reminder dispatch failed.");
                }
            }
            else
            {
                var errorCode = failure?.ErrorCode ?? BillReminderErrors.ReminderDispatchFailed;
                var errorMessage = failure?.ErrorMessage ?? "Reminder dispatch failed.";
                logger.LogWarning(
                    "Bill reminder dispatch failed for {ReminderId} due {DueDateUtc}: {ErrorCode} {ErrorMessage}",
                    reminder.Id.Value,
                    dueDateUtc,
                    errorCode,
                    errorMessage);
                reminder.MarkDispatchFailure(
                    nowUtc,
                    CalculateRetryDelay(reminder.DispatchAttemptCount),
                    errorCode,
                    errorMessage);
                await reminderRepository.UpdateAsync(reminder, cancellationToken);
                await reminderRepository.RecordDispatchAsync(
                    new BillReminderDispatchRecord(
                        reminder.Id,
                        dueDateUtc,
                        nowUtc,
                        false,
                        errorCode,
                        errorMessage),
                    cancellationToken);
                failed += 1;
            }
        }

        return new DispatchDueRemindersResult(attempted, sent, failed);
    }

    private static TimeSpan CalculateRetryDelay(int attemptCount)
    {
        var minutes = Math.Min(60, 5 * (attemptCount + 1));
        return TimeSpan.FromMinutes(minutes);
    }
}
