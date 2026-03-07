namespace MoneyTracker.Modules.Experiments.Domain;

public sealed class Experiment
{
    public ExperimentId Id { get; }
    public string Name { get; }
    public string Description { get; }
    public IReadOnlyList<ExperimentVariant> Variants { get; }
    public ExperimentStatus Status { get; private set; }
    public string TargetMetric { get; }
    public DateTimeOffset StartDate { get; }
    public DateTimeOffset EndDate { get; }

    private Experiment(
        ExperimentId id,
        string name,
        string description,
        IReadOnlyList<ExperimentVariant> variants,
        ExperimentStatus status,
        string targetMetric,
        DateTimeOffset startDate,
        DateTimeOffset endDate)
    {
        Id = id;
        Name = name;
        Description = description;
        Variants = variants;
        Status = status;
        TargetMetric = targetMetric;
        StartDate = startDate;
        EndDate = endDate;
    }

    public static Experiment Create(
        string name,
        string description,
        IReadOnlyList<ExperimentVariant> variants,
        string targetMetric,
        DateTimeOffset startDate,
        DateTimeOffset endDate)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ExperimentDomainException(
                ExperimentErrors.ValidationError,
                "Experiment name is required.");
        }

        if (variants is null || variants.Count == 0)
        {
            throw new ExperimentDomainException(
                ExperimentErrors.ValidationError,
                "At least one variant is required.");
        }

        var totalWeight = 0;
        foreach (var variant in variants)
        {
            totalWeight += variant.Weight;
        }

        if (totalWeight != 100)
        {
            throw new ExperimentDomainException(
                ExperimentErrors.VariantWeightsInvalid,
                $"Variant weights must sum to 100, but got {totalWeight}.");
        }

        return new Experiment(
            ExperimentId.New(),
            name,
            description ?? string.Empty,
            variants,
            ExperimentStatus.Draft,
            targetMetric ?? string.Empty,
            startDate,
            endDate);
    }

    public void Activate()
    {
        EnsureTransitionAllowed(ExperimentStatus.Active);
        Status = ExperimentStatus.Active;
    }

    public void Pause()
    {
        EnsureTransitionAllowed(ExperimentStatus.Paused);
        Status = ExperimentStatus.Paused;
    }

    public void Resume()
    {
        EnsureTransitionAllowed(ExperimentStatus.Active);
        Status = ExperimentStatus.Active;
    }

    public void Complete()
    {
        EnsureTransitionAllowed(ExperimentStatus.Completed);
        Status = ExperimentStatus.Completed;
    }

    public void UpdateStatus(ExperimentStatus targetStatus)
    {
        EnsureTransitionAllowed(targetStatus);
        Status = targetStatus;
    }

    private void EnsureTransitionAllowed(ExperimentStatus target)
    {
        var allowed = (Status, target) switch
        {
            (ExperimentStatus.Draft, ExperimentStatus.Active) => true,
            (ExperimentStatus.Active, ExperimentStatus.Paused) => true,
            (ExperimentStatus.Active, ExperimentStatus.Completed) => true,
            (ExperimentStatus.Paused, ExperimentStatus.Active) => true,
            _ => false
        };

        if (!allowed)
        {
            throw new ExperimentDomainException(
                ExperimentErrors.ExperimentInvalidStateTransition,
                $"Cannot transition from {Status} to {target}.");
        }
    }
}
