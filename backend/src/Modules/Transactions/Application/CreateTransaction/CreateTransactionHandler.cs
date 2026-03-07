using MoneyTracker.Modules.Budgets.Domain;
using MoneyTracker.Modules.SharedKernel.Households;
using MoneyTracker.Modules.Transactions.Domain;

namespace MoneyTracker.Modules.Transactions.Application.CreateTransaction;

public sealed class CreateTransactionHandler(
    ITransactionRepository transactionRepository,
    IBudgetRepository budgetRepository,
    IHouseholdAccessService householdAccessService,
    TimeProvider timeProvider)
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
        return CreateTransactionResult.Success(transaction);
    }
}
