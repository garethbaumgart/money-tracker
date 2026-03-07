using MoneyTracker.Modules.Feedback.Domain;

namespace MoneyTracker.Modules.Feedback.Application.SubmitNps;

public sealed class SubmitNpsHandler(
    INpsRepository npsRepository,
    TimeProvider timeProvider)
{
    public async Task<SubmitNpsResult> HandleAsync(
        SubmitNpsCommand command,
        CancellationToken cancellationToken)
    {
        NpsScore npsScore;
        try
        {
            npsScore = NpsScore.Create(
                command.UserId,
                command.Score,
                command.Comment,
                timeProvider.GetUtcNow());
        }
        catch (FeedbackDomainException ex)
        {
            return SubmitNpsResult.Validation(ex.Code, ex.Message);
        }

        await npsRepository.AddAsync(npsScore, cancellationToken);

        return SubmitNpsResult.Success(npsScore.Id);
    }
}
