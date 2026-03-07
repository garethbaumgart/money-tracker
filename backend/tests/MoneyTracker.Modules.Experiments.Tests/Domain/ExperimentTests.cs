using MoneyTracker.Modules.Experiments.Domain;

namespace MoneyTracker.Modules.Experiments.Tests.Domain;

public sealed class ExperimentTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Create_WithWeightsSummingTo100_Succeeds_WithDraftStatus()
    {
        var variants = new List<ExperimentVariant>
        {
            new("Control", 50),
            new("Treatment", 50)
        };

        var experiment = Experiment.Create(
            "Onboarding Test",
            "Test onboarding flow",
            variants,
            "conversion_rate",
            DateTimeOffset.Parse("2026-03-01T00:00:00Z"),
            DateTimeOffset.Parse("2026-04-01T00:00:00Z"));

        Assert.NotEqual(Guid.Empty, experiment.Id.Value);
        Assert.Equal("Onboarding Test", experiment.Name);
        Assert.Equal(ExperimentStatus.Draft, experiment.Status);
        Assert.Equal(2, experiment.Variants.Count);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Create_WithWeightsSummingTo80_ThrowsExperimentDomainException()
    {
        var variants = new List<ExperimentVariant>
        {
            new("Control", 50),
            new("Treatment", 30)
        };

        var exception = Assert.Throws<ExperimentDomainException>(() =>
            Experiment.Create(
                "Bad Test",
                "Invalid weights",
                variants,
                "conversion_rate",
                DateTimeOffset.Parse("2026-03-01T00:00:00Z"),
                DateTimeOffset.Parse("2026-04-01T00:00:00Z")));

        Assert.Equal(ExperimentErrors.VariantWeightsInvalid, exception.Code);
        Assert.Contains("80", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Activate_FromDraft_Succeeds()
    {
        var experiment = CreateValidExperiment();
        Assert.Equal(ExperimentStatus.Draft, experiment.Status);

        experiment.Activate();

        Assert.Equal(ExperimentStatus.Active, experiment.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Pause_FromActive_Succeeds()
    {
        var experiment = CreateValidExperiment();
        experiment.Activate();

        experiment.Pause();

        Assert.Equal(ExperimentStatus.Paused, experiment.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Resume_FromPaused_Succeeds()
    {
        var experiment = CreateValidExperiment();
        experiment.Activate();
        experiment.Pause();

        experiment.Resume();

        Assert.Equal(ExperimentStatus.Active, experiment.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Complete_FromActive_Succeeds()
    {
        var experiment = CreateValidExperiment();
        experiment.Activate();

        experiment.Complete();

        Assert.Equal(ExperimentStatus.Completed, experiment.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Activate_FromCompleted_Throws()
    {
        var experiment = CreateValidExperiment();
        experiment.Activate();
        experiment.Complete();

        var exception = Assert.Throws<ExperimentDomainException>(() =>
            experiment.Activate());

        Assert.Equal(ExperimentErrors.ExperimentInvalidStateTransition, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Pause_FromDraft_Throws()
    {
        var experiment = CreateValidExperiment();

        var exception = Assert.Throws<ExperimentDomainException>(() =>
            experiment.Pause());

        Assert.Equal(ExperimentErrors.ExperimentInvalidStateTransition, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Complete_FromDraft_Throws()
    {
        var experiment = CreateValidExperiment();

        var exception = Assert.Throws<ExperimentDomainException>(() =>
            experiment.Complete());

        Assert.Equal(ExperimentErrors.ExperimentInvalidStateTransition, exception.Code);
    }

    private static Experiment CreateValidExperiment()
    {
        var variants = new List<ExperimentVariant>
        {
            new("Control", 50),
            new("Treatment", 50)
        };

        return Experiment.Create(
            "Test Experiment",
            "Description",
            variants,
            "conversion_rate",
            DateTimeOffset.Parse("2026-03-01T00:00:00Z"),
            DateTimeOffset.Parse("2026-04-01T00:00:00Z"));
    }
}
