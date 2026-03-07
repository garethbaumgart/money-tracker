using MoneyTracker.Modules.Budgets.Domain;
using MoneyTracker.Modules.SharedKernel.Analytics;
using MoneyTracker.Modules.SharedKernel.Households;
using MoneyTracker.Modules.Transactions.Domain;

namespace MoneyTracker.Modules.Transactions.Application.CreateTransaction;

public sealed class CreateTransactionHandler(
    ITransactionRepository transactionRepository,
    IBudgetRepository budgetRepository,
    IHouseholdAccessService householdAccessService,
    TimeProvider timeProvider,
    IAnalyticsEventPublisher analyticsPublisher)
{
    public async Task<CreateTransactionResult> HandleAsync(
        CreateTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var access = await householdAccessService.CheckMemberAsync(
            command.HouseholdId,
            command.RequestingUserId,
            cancellationToken);
        if (!access.HouseholdExists)
        {
            return CreateTransactionResult.HouseholdNotFound();
        }

        if (!access.IsMember)
        {
            return CreateTransactionResult.AccessDenied();
        }

        if (command.CategoryId.HasValue)
        {
            var category = await budgetRepository.GetCategoryAsync(
                command.HouseholdId,
                new BudgetCategoryId(command.CategoryId.Value),
                cancellationToken);
            if (category is null)
            {
                return CreateTransactionResult.CategoryNotFound();
            }
        }

        Transaction transaction;
        try
        {
            transaction = Transaction.Create(
                command.HouseholdId,
                command.RequestingUserId,
                command.Amount,
                command.OccurredAtUtc,
                command.Description,
                command.CategoryId,
                timeProvider.GetUtcNow());
        }
        catch (TransactionDomainException exception)
        {
            return CreateTransactionResult.Validation(exception.Code, exception.Message);
        }

        await transactionRepository.AddAsync(transaction, cancellationToken);

        await analyticsPublisher.PublishAsync(
            command.RequestingUserId, "first_transaction_created", command.HouseholdId, cancellationToken);

        return CreateTransactionResult.Success(transaction);
    }
}
