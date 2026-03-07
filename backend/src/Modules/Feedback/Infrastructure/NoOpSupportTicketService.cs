using MoneyTracker.Modules.Feedback.Domain;

namespace MoneyTracker.Modules.Feedback.Infrastructure;

public sealed class NoOpSupportTicketService : ISupportTicketService
{
    public Task CreateTicketAsync(
        string subject,
        string description,
        string priority,
        CancellationToken cancellationToken)
    {
        // No-op stub for testing. Will be replaced with real support ticket integration.
        return Task.CompletedTask;
    }
}
