namespace MoneyTracker.Modules.Experiments.Domain;

public static class ExperimentErrors
{
    public const string ValidationError = "experiment_validation_error";
    public const string ExperimentNotFound = "experiment_not_found";
    public const string ExperimentNotActive = "experiment_not_active";
    public const string ExperimentInvalidStateTransition = "experiment_invalid_state_transition";
    public const string VariantWeightsInvalid = "experiment_variant_weights_invalid";
    public const string AllocationNotFound = "experiment_allocation_not_found";
    public const string AccessDenied = "experiment_access_denied";
}
