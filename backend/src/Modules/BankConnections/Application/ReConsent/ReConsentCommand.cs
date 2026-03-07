using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Application.ReConsent;

public sealed record ReConsentCommand(
    BankConnectionId ConnectionId,
    Guid RequestingUserId);
