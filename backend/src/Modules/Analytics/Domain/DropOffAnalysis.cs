namespace MoneyTracker.Modules.Analytics.Domain;

public sealed record DropOffAnalysis(
    string FromStage,
    string ToStage,
    double DropOffRate,
    int LostUsers);
