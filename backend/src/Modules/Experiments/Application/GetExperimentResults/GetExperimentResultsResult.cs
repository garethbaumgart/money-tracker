namespace MoneyTracker.Modules.Experiments.Application.GetExperimentResults;

public sealed class GetExperimentResultsResult
{
    private GetExperimentResultsResult(
        Guid? experimentId,
        string? experimentName,
        IReadOnlyList<VariantResultDto>? variants,
        double? chiSquaredStatistic,
        double? pValue,
        bool? isSignificant,
        bool? sampleSizeWarning,
        string? errorCode,
        string? errorMessage)
    {
        ExperimentId = experimentId;
        ExperimentName = experimentName;
        Variants = variants;
        ChiSquaredStatistic = chiSquaredStatistic;
        PValue = pValue;
        IsSignificant = isSignificant;
        SampleSizeWarning = sampleSizeWarning;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public Guid? ExperimentId { get; }
    public string? ExperimentName { get; }
    public IReadOnlyList<VariantResultDto>? Variants { get; }
    public double? ChiSquaredStatistic { get; }
    public double? PValue { get; }
    public bool? IsSignificant { get; }
    public bool? SampleSizeWarning { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public bool IsSuccess => ErrorCode is null;

    public static GetExperimentResultsResult Success(
        Guid experimentId,
        string experimentName,
        IReadOnlyList<VariantResultDto> variants,
        double chiSquaredStatistic,
        double pValue,
        bool isSignificant,
        bool sampleSizeWarning)
    {
        return new GetExperimentResultsResult(
            experimentId, experimentName, variants,
            chiSquaredStatistic, pValue, isSignificant, sampleSizeWarning,
            null, null);
    }

    public static GetExperimentResultsResult Error(string errorCode, string errorMessage)
    {
        return new GetExperimentResultsResult(
            null, null, null, null, null, null, null,
            errorCode, errorMessage);
    }
}

public sealed record VariantResultDto(
    string VariantName,
    int TotalAllocations,
    int Conversions,
    double ConversionRate);
