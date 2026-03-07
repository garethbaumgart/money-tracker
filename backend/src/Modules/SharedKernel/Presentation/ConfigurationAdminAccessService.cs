using Microsoft.Extensions.Configuration;

namespace MoneyTracker.Modules.SharedKernel.Presentation;

/// <summary>
/// Checks admin access by comparing the user ID against a configured list of admin user IDs.
/// Configure via "Admin:UserIds" as a semicolon-separated list of GUIDs, or use "*" to
/// grant admin access to all authenticated users (useful for local development and testing).
/// </summary>
public sealed class ConfigurationAdminAccessService : IAdminAccessService
{
    private readonly bool _allowAll;
    private readonly HashSet<Guid> _adminUserIds;

    public ConfigurationAdminAccessService(IConfiguration configuration)
    {
        _adminUserIds = [];
        var raw = configuration["Admin:UserIds"];

        if (raw is "*")
        {
            _allowAll = true;
            return;
        }

        if (!string.IsNullOrWhiteSpace(raw))
        {
            foreach (var segment in raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Guid.TryParse(segment, out var id))
                {
                    _adminUserIds.Add(id);
                }
            }
        }
    }

    public Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_allowAll || _adminUserIds.Contains(userId));
    }
}
