using MoneyTracker.Modules.Experiments.Domain;
using MoneyTracker.Modules.Experiments.Infrastructure;

namespace MoneyTracker.Modules.Experiments.Application.GetExperimentResults;

public sealed class GetExperimentResultsHandler(IExperimentRepository repository)
{
    public async Task<GetExperimentResultsResult> HandleAsync(
        GetExperimentResultsQuery query,
        CancellationToken cancellationToken)
    {
        var experiment = await repository.GetExperimentByIdAsync(query.ExperimentId, cancellationToken);
        if (experiment is null)
        {
            return GetExperimentResultsResult.Error(
                ExperimentErrors.ExperimentNotFound,
                "Experiment not found.");
        }

        var allocations = await repository.GetAllocationsByExperimentAsync(query.ExperimentId, cancellationToken);
        var conversions = await repository.GetConversionEventsByExperimentAsync(query.ExperimentId, cancellationToken);

        var conversionsByVariant = conversions
            .GroupBy(c => c.VariantName)
            .ToDictionary(g => g.Key, g => g.Count());

        var allocationsByVariant = allocations
            .GroupBy(a => a.VariantName)
            .ToDictionary(g => g.Key, g => g.Count());

        var variantResults = new List<VariantResultDto>();
        foreach (var variant in experiment.Variants)
        {
            var totalAllocations = allocationsByVariant.GetValueOrDefault(variant.Name, 0);
            var totalConversions = conversionsByVariant.GetValueOrDefault(variant.Name, 0);
            var conversionRate = totalAllocations > 0
                ? (double)totalConversions / totalAllocations
                : 0.0;

            variantResults.Add(new VariantResultDto(
                variant.Name,
                totalAllocations,
                totalConversions,
                conversionRate));
        }

        // Calculate chi-squared significance (only meaningful with 2 variants)
        double chiSquaredStatistic = 0;
        double pValue = 1.0;
        bool isSignificant = false;
        bool sampleSizeWarning = true;

        if (variantResults.Count >= 2)
        {
            var a = variantResults[0];
            var b = variantResults[1];

            var significance = ChiSquaredSignificanceCalculator.Calculate(
                a.TotalAllocations,
                a.Conversions,
                b.TotalAllocations,
                b.Conversions);

            chiSquaredStatistic = significance.ChiSquaredStatistic;
            pValue = significance.PValue;
            isSignificant = significance.IsSignificant;
            sampleSizeWarning = significance.SampleSizeWarning;
        }

        return GetExperimentResultsResult.Success(
            experiment.Id.Value,
            experiment.Name,
            variantResults,
            chiSquaredStatistic,
            pValue,
            isSignificant,
            sampleSizeWarning);
    }
}
