using MoneyTracker.Modules.Budgets.Domain;
using MoneyTracker.Modules.SharedKernel.Households;
using MoneyTracker.Modules.Transactions.Domain;

namespace MoneyTracker.Modules.Transactions.Application.GetTransactions;

public sealed class GetTransactionsHandler(
    ITransactionRepository transactionRepository,
    IBudgetRepository budgetRepository,
    IHouseholdAccessService householdAccessService)
{
    public async Task<GetTransactionsResult> HandleAsync(
        GetTransactionsQuery query,
        CancellationToken cancellationToken)
    {
        var access = await householdAccessService.CheckMemberAsync(
            query.HouseholdId,
            query.RequestingUserId,
            cancellationToken);
        if (!access.HouseholdExists)
        {
            return GetTransactionsResult.HouseholdNotFound();
        }

        if (!access.IsMember)
        {
            return GetTransactionsResult.AccessDenied();
        }

        var transactions = await transactionRepository.GetByHouseholdAsync(
            query.HouseholdId,
            query.FromUtc,
            query.ToUtc,
            cancellationToken);
        var categories = await budgetRepository.GetCategoriesAsync(query.HouseholdId, cancellationToken);
        var categoryLookup = categories.ToDictionary(category => category.Id.Value, category => category.Name);

        var summaries = transactions
            .OrderByDescending(transaction => transaction.OccurredAtUtc)
            .Select(transaction => new TransactionSummary(
                transaction.Id.Value,
                transaction.HouseholdId,
                transaction.Amount,
                transaction.OccurredAtUtc,
                transaction.Description,
                transaction.CategoryId,
                transaction.CategoryId.HasValue && categoryLookup.TryGetValue(transaction.CategoryId.Value, out var name)
                    ? name
                    : null,
                transaction.CreatedAtUtc))
            .ToArray();

        return GetTransactionsResult.Success(summaries);
    }
}
