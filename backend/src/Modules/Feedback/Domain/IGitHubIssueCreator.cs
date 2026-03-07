namespace MoneyTracker.Modules.Feedback.Domain;

public interface IGitHubIssueCreator
{
    Task CreateIssueAsync(
        string title,
        string body,
        string[] labels,
        CancellationToken cancellationToken);
}
