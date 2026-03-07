namespace MoneyTracker.Modules.Auth.Application.ExportUserData;

public sealed class ExportUserDataHandler
{
    public async Task<ExportUserDataResult> HandleAsync(
        ExportUserDataQuery query,
        IReadOnlyList<ExportParticipantEntry> participants,
        CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, object>();

        foreach (var entry in participants)
        {
            var participantData = await entry.ExportAsync(query.UserId, cancellationToken);
            data[entry.ModuleName] = participantData;
        }

        return ExportUserDataResult.Success(data);
    }
}

public sealed record ExportParticipantEntry(string ModuleName, Func<Guid, CancellationToken, Task<object>> ExportAsync);
