namespace MoneyTracker.Modules.SharedKernel.Presentation;

/// <summary>
/// Test stub that grants admin access to all users.
/// </summary>
public sealed class AllowAllAdminAccessService : IAdminAccessService
{
    public Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }
}
