using MoneyTracker.Modules.Feedback.Domain;

namespace MoneyTracker.Modules.Feedback.Infrastructure;

public sealed class NoOpGitHubIssueCreator : IGitHubIssueCreator
{
    public Task CreateIssueAsync(
        string title,
        string body,
        string[] labels,
        CancellationToken cancellationToken)
    {
        // No-op stub for testing. Will be replaced with real GitHub integration.
        return Task.CompletedTask;
    }
}
