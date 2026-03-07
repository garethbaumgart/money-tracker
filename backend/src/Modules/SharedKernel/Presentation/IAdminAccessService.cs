namespace MoneyTracker.Modules.SharedKernel.Presentation;

public interface IAdminAccessService
{
    Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken);
}
