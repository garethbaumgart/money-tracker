using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Application.ReConsent;

public sealed class ReConsentResult
{
    private ReConsentResult(
        string? consentUrl,
        string? consentSessionId,
        string? errorCode,
        string? errorMessage)
    {
        ConsentUrl = consentUrl;
        ConsentSessionId = consentSessionId;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public string? ConsentUrl { get; }

    public string? ConsentSessionId { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess => ConsentUrl is not null;

    public static ReConsentResult Success(string consentUrl, string consentSessionId)
    {
        return new ReConsentResult(consentUrl, consentSessionId, errorCode: null, errorMessage: null);
    }

    public static ReConsentResult ConnectionNotFound()
    {
        return new ReConsentResult(
            consentUrl: null,
            consentSessionId: null,
            BankConnectionErrors.ConnectionNotFound,
            "Bank connection not found.");
    }

    public static ReConsentResult AccessDenied()
    {
        return new ReConsentResult(
            consentUrl: null,
            consentSessionId: null,
            BankConnectionErrors.ConnectionAccessDenied,
            "User is not a member of this household.");
    }

    public static ReConsentResult ReConsentNotNeeded()
    {
        return new ReConsentResult(
            consentUrl: null,
            consentSessionId: null,
            BankConnectionErrors.ReConsentNotNeeded,
            "Connection is still active and does not require re-consent.");
    }

    public static ReConsentResult ProviderError(string? errorCode, string? errorMessage)
    {
        return new ReConsentResult(
            consentUrl: null,
            consentSessionId: null,
            errorCode ?? BankConnectionErrors.ConnectionProviderError,
            errorMessage ?? "Bank provider returned an error.");
    }
}
