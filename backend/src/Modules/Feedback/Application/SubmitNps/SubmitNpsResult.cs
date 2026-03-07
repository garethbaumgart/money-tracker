namespace MoneyTracker.Modules.Feedback.Application.SubmitNps;

public sealed class SubmitNpsResult
{
    private SubmitNpsResult(
        Guid? npsId,
        string? errorCode,
        string? errorMessage)
    {
        NpsId = npsId;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public Guid? NpsId { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public bool IsSuccess => NpsId is not null;

    public static SubmitNpsResult Success(Guid npsId)
    {
        return new SubmitNpsResult(npsId, errorCode: null, errorMessage: null);
    }

    public static SubmitNpsResult Validation(string code, string message)
    {
        return new SubmitNpsResult(npsId: null, code, message);
    }
}
