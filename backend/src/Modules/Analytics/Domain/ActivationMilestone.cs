namespace MoneyTracker.Modules.Analytics.Domain;

public enum ActivationMilestone
{
    SignupCompleted,
    HouseholdCreated,
    PartnerInvited,
    PartnerJoined,
    FirstBudgetCreated,
    FirstTransactionCreated,
    BankLinkStarted,
    BankLinkCompleted,
    FirstSyncCompleted,
    PaywallViewed,
    TrialStarted,
    PaidConversion
}

public static class ActivationMilestoneExtensions
{
    private static readonly Dictionary<string, ActivationMilestone> NameLookup =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["signup_completed"] = ActivationMilestone.SignupCompleted,
            ["household_created"] = ActivationMilestone.HouseholdCreated,
            ["partner_invited"] = ActivationMilestone.PartnerInvited,
            ["partner_joined"] = ActivationMilestone.PartnerJoined,
            ["first_budget_created"] = ActivationMilestone.FirstBudgetCreated,
            ["first_transaction_created"] = ActivationMilestone.FirstTransactionCreated,
            ["bank_link_started"] = ActivationMilestone.BankLinkStarted,
            ["bank_link_completed"] = ActivationMilestone.BankLinkCompleted,
            ["first_sync_completed"] = ActivationMilestone.FirstSyncCompleted,
            ["paywall_viewed"] = ActivationMilestone.PaywallViewed,
            ["trial_started"] = ActivationMilestone.TrialStarted,
            ["paid_conversion"] = ActivationMilestone.PaidConversion,
        };

    private static readonly Dictionary<ActivationMilestone, string> ValueLookup =
        NameLookup.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    public static bool TryParse(string name, out ActivationMilestone milestone)
    {
        return NameLookup.TryGetValue(name, out milestone);
    }

    public static string ToSnakeCase(this ActivationMilestone milestone)
    {
        return ValueLookup[milestone];
    }

    public static IReadOnlyList<ActivationMilestone> OrderedStages { get; } =
    [
        ActivationMilestone.SignupCompleted,
        ActivationMilestone.HouseholdCreated,
        ActivationMilestone.PartnerInvited,
        ActivationMilestone.PartnerJoined,
        ActivationMilestone.FirstBudgetCreated,
        ActivationMilestone.FirstTransactionCreated,
        ActivationMilestone.BankLinkStarted,
        ActivationMilestone.BankLinkCompleted,
        ActivationMilestone.FirstSyncCompleted,
        ActivationMilestone.PaywallViewed,
        ActivationMilestone.TrialStarted,
        ActivationMilestone.PaidConversion,
    ];
}
