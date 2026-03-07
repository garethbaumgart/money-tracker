namespace MoneyTracker.Modules.Experiments.Application.RecordConversion;

public sealed class RecordConversionResult
{
    private RecordConversionResult(string? errorCode, string? errorMessage)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public bool IsSuccess => ErrorCode is null;

    public static RecordConversionResult Success()
    {
        return new RecordConversionResult(null, null);
    }

    public static RecordConversionResult Error(string errorCode, string errorMessage)
    {
        return new RecordConversionResult(errorCode, errorMessage);
    }
}
