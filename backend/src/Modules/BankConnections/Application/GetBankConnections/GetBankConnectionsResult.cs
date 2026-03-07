using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Application.GetBankConnections;

public sealed class GetBankConnectionsResult
{
    private GetBankConnectionsResult(
        IReadOnlyCollection<BankConnectionSummary>? connections,
        string? errorCode,
        string? errorMessage)
    {
        Connections = connections;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public IReadOnlyCollection<BankConnectionSummary>? Connections { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess => Connections is not null;

    public static GetBankConnectionsResult Success(IReadOnlyCollection<BankConnectionSummary> connections)
    {
        return new GetBankConnectionsResult(connections, errorCode: null, errorMessage: null);
    }

    public static GetBankConnectionsResult AccessDenied()
    {
        return new GetBankConnectionsResult(
            connections: null,
            BankConnectionErrors.ConnectionAccessDenied,
            "User is not a member of this household.");
    }

    public static GetBankConnectionsResult HouseholdNotFound()
    {
        return new GetBankConnectionsResult(
            connections: null,
            BankConnectionErrors.ConnectionHouseholdNotFound,
            "Household not found.");
    }
}

public sealed record BankConnectionSummary(
    Guid Id,
    Guid HouseholdId,
    string? InstitutionName,
    string Status,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
