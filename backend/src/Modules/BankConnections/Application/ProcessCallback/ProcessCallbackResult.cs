using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Application.ProcessCallback;

public sealed class ProcessCallbackResult
{
    private ProcessCallbackResult(
        BankConnection? connection,
        string? errorCode,
        string? errorMessage)
    {
        Connection = connection;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public BankConnection? Connection { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess => Connection is not null && Connection.Status == BankConnectionStatus.Active;

    public bool IsFailedConnection => Connection is not null && Connection.Status == BankConnectionStatus.Failed;

    public static ProcessCallbackResult Success(BankConnection connection)
    {
        return new ProcessCallbackResult(connection, errorCode: null, errorMessage: null);
    }

    public static ProcessCallbackResult Failed(BankConnection connection, string errorCode, string errorMessage)
    {
        return new ProcessCallbackResult(connection, errorCode, errorMessage);
    }

    public static ProcessCallbackResult Validation(string code, string message)
    {
        return new ProcessCallbackResult(connection: null, code, message);
    }

    public static ProcessCallbackResult ConnectionNotFound()
    {
        return new ProcessCallbackResult(
            connection: null,
            BankConnectionErrors.ConnectionNotFound,
            "No pending connection found for this session.");
    }
}
