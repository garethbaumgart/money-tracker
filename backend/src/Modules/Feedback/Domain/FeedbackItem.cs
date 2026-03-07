namespace MoneyTracker.Modules.Feedback.Domain;

public sealed class FeedbackItem
{
    public FeedbackId Id { get; }
    public Guid UserId { get; }
    public FeedbackCategory Category { get; }
    public string Description { get; }
    public int Rating { get; }
    public FeedbackMetadata Metadata { get; }
    public PriorityScore PriorityScore { get; private set; }
    public FeedbackStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? TriagedAtUtc { get; private set; }

    private FeedbackItem(
        FeedbackId id,
        Guid userId,
        FeedbackCategory category,
        string description,
        int rating,
        FeedbackMetadata metadata,
        PriorityScore priorityScore,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        UserId = userId;
        Category = category;
        Description = description;
        Rating = rating;
        Metadata = metadata;
        PriorityScore = priorityScore;
        Status = FeedbackStatus.New;
        CreatedAtUtc = createdAtUtc;
    }

    public static FeedbackItem Create(
        Guid userId,
        FeedbackCategory category,
        string description,
        int rating,
        FeedbackMetadata metadata,
        PriorityScore priorityScore,
        DateTimeOffset nowUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new FeedbackDomainException(
                FeedbackErrors.ValidationError,
                "User ID is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new FeedbackDomainException(
                FeedbackErrors.ValidationError,
                "Description is required.");
        }

        if (description.Length > 5000)
        {
            throw new FeedbackDomainException(
                FeedbackErrors.ValidationError,
                "Description exceeds maximum length of 5000 characters.");
        }

        if (rating < 1 || rating > 5)
        {
            throw new FeedbackDomainException(
                FeedbackErrors.ValidationError,
                "Rating must be between 1 and 5.");
        }

        return new FeedbackItem(
            FeedbackId.New(),
            userId,
            category,
            description.Trim(),
            rating,
            metadata,
            priorityScore,
            nowUtc);
    }

    public void Triage(FeedbackStatus newStatus, PriorityScore? priorityOverride, DateTimeOffset nowUtc)
    {
        if (newStatus == FeedbackStatus.New)
        {
            throw new FeedbackDomainException(
                FeedbackErrors.InvalidStatusTransition,
                "Cannot triage feedback back to New status.");
        }

        Status = newStatus;
        TriagedAtUtc = nowUtc;

        if (priorityOverride is not null)
        {
            PriorityScore = priorityOverride;
        }
    }
}
