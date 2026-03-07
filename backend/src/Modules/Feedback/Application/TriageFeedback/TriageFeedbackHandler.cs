using MoneyTracker.Modules.Feedback.Domain;

namespace MoneyTracker.Modules.Feedback.Application.TriageFeedback;

public sealed class TriageFeedbackHandler(
    IFeedbackRepository feedbackRepository,
    TimeProvider timeProvider)
{
    public async Task<TriageFeedbackResult> HandleAsync(
        TriageFeedbackCommand command,
        CancellationToken cancellationToken)
    {
        var feedback = await feedbackRepository.GetByIdAsync(
            command.FeedbackId,
            cancellationToken);

        if (feedback is null)
        {
            return TriageFeedbackResult.NotFound();
        }

        var priorityOverride = command.PriorityOverride.HasValue
            ? PriorityScore.Compute(command.PriorityOverride.Value)
            : null;

        try
        {
            feedback.Triage(command.Status, priorityOverride, timeProvider.GetUtcNow());
        }
        catch (FeedbackDomainException ex)
        {
            return TriageFeedbackResult.Validation(ex.Code, ex.Message);
        }

        await feedbackRepository.UpdateAsync(feedback, cancellationToken);

        return TriageFeedbackResult.Success();
    }
}
