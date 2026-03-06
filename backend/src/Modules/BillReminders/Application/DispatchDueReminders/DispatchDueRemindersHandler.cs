using MoneyTracker.Modules.BillReminders.Domain;
using MoneyTracker.Modules.Households.Domain;
using MoneyTracker.Modules.Notifications.Domain;

namespace MoneyTracker.Modules.BillReminders.Application.DispatchDueReminders;

public sealed class DispatchDueRemindersHandler(
    IBillReminderRepository reminderRepository,
    IHouseholdRepository householdRepository,
    INotificationTokenRepository notificationTokenRepository,
    INotificationSender notificationSender,
    TimeProvider timeProvider)
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
            if (await reminderRepository.HasDispatchRecordAsync(
                    reminder.Id,
                    reminder.NextDueDateUtc,
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
                reminder.MarkDispatchFailure(
                    nowUtc,
                    CalculateRetryDelay(reminder.DispatchAttemptCount),
                    BillReminderErrors.ReminderDispatchFailed,
                    "Household not found for reminder dispatch.");
                await reminderRepository.UpdateAsync(reminder, cancellationToken);
                await reminderRepository.RecordDispatchAsync(
                    new BillReminderDispatchRecord(
                        reminder.Id,
                        reminder.NextDueDateUtc,
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
                reminder.MarkDispatchFailure(
                    nowUtc,
                    CalculateRetryDelay(reminder.DispatchAttemptCount),
                    BillReminderErrors.ReminderDispatchFailed,
                    "No device tokens registered for reminder dispatch.");
                await reminderRepository.UpdateAsync(reminder, cancellationToken);
                await reminderRepository.RecordDispatchAsync(
                    new BillReminderDispatchRecord(
                        reminder.Id,
                        reminder.NextDueDateUtc,
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
                reminder.NextDueDateUtc);

            NotificationDispatchResult? failure = null;
            foreach (var token in tokens)
            {
                var dispatchResult = await notificationSender.SendReminderAsync(message, token, cancellationToken);
                if (!dispatchResult.IsSuccess)
                {
                    failure = dispatchResult;
                    break;
                }
            }

            if (failure is null)
            {
                reminder.MarkDispatchSuccess(nowUtc);
                await reminderRepository.UpdateAsync(reminder, cancellationToken);
                await reminderRepository.RecordDispatchAsync(
                    new BillReminderDispatchRecord(
                        reminder.Id,
                        reminder.LastNotifiedDueDateUtc!.Value,
                        nowUtc,
                        true,
                        errorCode: null,
                        errorMessage: null),
                    cancellationToken);
                sent += 1;
            }
            else
            {
                reminder.MarkDispatchFailure(
                    nowUtc,
                    CalculateRetryDelay(reminder.DispatchAttemptCount),
                    failure.ErrorCode ?? BillReminderErrors.ReminderDispatchFailed,
                    failure.ErrorMessage ?? "Reminder dispatch failed.");
                await reminderRepository.UpdateAsync(reminder, cancellationToken);
                await reminderRepository.RecordDispatchAsync(
                    new BillReminderDispatchRecord(
                        reminder.Id,
                        reminder.NextDueDateUtc,
                        nowUtc,
                        false,
                        failure.ErrorCode ?? BillReminderErrors.ReminderDispatchFailed,
                        failure.ErrorMessage ?? "Reminder dispatch failed."),
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
