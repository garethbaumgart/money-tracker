namespace MoneyTracker.Modules.Feedback.Domain;

public sealed class NpsScore
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public int Score { get; }
    public string? Comment { get; }
    public NpsCategory Category { get; }
    public DateTimeOffset RecordedAtUtc { get; }

    private NpsScore(
        Guid id,
        Guid userId,
        int score,
        string? comment,
        NpsCategory category,
        DateTimeOffset recordedAtUtc)
    {
        Id = id;
        UserId = userId;
        Score = score;
        Comment = comment;
        Category = category;
        RecordedAtUtc = recordedAtUtc;
    }

    public static NpsScore Create(
        Guid userId,
        int score,
        string? comment,
        DateTimeOffset nowUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new FeedbackDomainException(
                FeedbackErrors.ValidationError,
                "User ID is required.");
        }

        if (score < 0 || score > 10)
        {
            throw new FeedbackDomainException(
                FeedbackErrors.NpsScoreOutOfRange,
                "NPS score must be between 0 and 10.");
        }

        if (comment is not null && comment.Length > 1000)
        {
            throw new FeedbackDomainException(
                FeedbackErrors.ValidationError,
                "Comment exceeds maximum length of 1000 characters.");
        }

        var category = score switch
        {
            >= 9 => NpsCategory.Promoter,
            >= 7 => NpsCategory.Passive,
            _ => NpsCategory.Detractor
        };

        return new NpsScore(
            Guid.NewGuid(),
            userId,
            score,
            comment?.Trim(),
            category,
            nowUtc);
    }
}
