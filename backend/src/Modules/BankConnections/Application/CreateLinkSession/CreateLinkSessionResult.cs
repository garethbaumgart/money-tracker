namespace MoneyTracker.Modules.BankConnections.Application.CreateLinkSession;

public sealed class CreateLinkSessionResult
{
    private CreateLinkSessionResult(
        string? consentUrl,
        Guid? connectionId,
        string? errorCode,
        string? errorMessage)
    {
        ConsentUrl = consentUrl;
        ConnectionId = connectionId;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public string? ConsentUrl { get; }

    public Guid? ConnectionId { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess => ConsentUrl is not null;

    public static CreateLinkSessionResult Success(string consentUrl, Guid connectionId)
    {
        return new CreateLinkSessionResult(consentUrl, connectionId, errorCode: null, errorMessage: null);
    }

    public static CreateLinkSessionResult Validation(string code, string message)
    {
        return new CreateLinkSessionResult(consentUrl: null, connectionId: null, code, message);
    }

    public static CreateLinkSessionResult AccessDenied()
    {
        return new CreateLinkSessionResult(
            consentUrl: null,
            connectionId: null,
            Domain.BankConnectionErrors.ConnectionAccessDenied,
            "User is not a member of this household.");
    }

    public static CreateLinkSessionResult HouseholdNotFound()
    {
        return new CreateLinkSessionResult(
            consentUrl: null,
            connectionId: null,
            Domain.BankConnectionErrors.ConnectionHouseholdNotFound,
            "Household not found.");
    }

    public static CreateLinkSessionResult ProviderError(string? errorCode, string? errorMessage)
    {
        return new CreateLinkSessionResult(
            consentUrl: null,
            connectionId: null,
            errorCode ?? Domain.BankConnectionErrors.ConnectionProviderError,
            errorMessage ?? "Bank provider returned an error.");
    }
}
