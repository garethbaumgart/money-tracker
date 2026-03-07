namespace MoneyTracker.Modules.Feedback.Domain;

public interface ISupportTicketService
{
    Task CreateTicketAsync(
        string subject,
        string description,
        string priority,
        CancellationToken cancellationToken);
}
