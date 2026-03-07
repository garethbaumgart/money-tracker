using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Modules.Budgets.Domain;
using MoneyTracker.Modules.Households.Application.GetCurrentBudgetSnapshot;

namespace MoneyTracker.Modules.Households.Presentation;

public static class BudgetSnapshotEndpoint
{
    public static IEndpointRouteBuilder MapBudgetSnapshotEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/budgets/current", GetCurrentBudgetSnapshot)
            .WithName("GetCurrentBudgetSnapshot")
            .WithSummary("Get the current budget snapshot.")
            .WithDescription("Returns budget allocations and transaction totals for the current month.");

        return app;
    }

    private static async Task GetCurrentBudgetSnapshot(HttpContext httpContext)
    {
        var authResult = await HouseholdEndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        if (!TryGetGuidQuery(httpContext, "householdId", out var householdId))
        {
            await HouseholdEndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "householdId query parameter is required.",
                BudgetErrors.ValidationError,
                BudgetErrors.ValidationError);
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<GetCurrentBudgetSnapshotHandler>();
        var result = await handler.HandleAsync(
            new GetCurrentBudgetSnapshotQuery(householdId, authResult.AuthenticatedUser!.UserId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                BudgetErrors.BudgetHouseholdNotFound => StatusCodes.Status404NotFound,
                BudgetErrors.BudgetAccessDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };

            await HouseholdEndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status403Forbidden ? "Access denied." : "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                BudgetErrors.ValidationError);
            return;
        }

        var snapshot = result.Snapshot!;
        var response = new CurrentBudgetSnapshotResponse(
            snapshot.HouseholdId,
            snapshot.PeriodStartUtc,
            snapshot.PeriodEndUtc,
            snapshot.TotalAllocated,
            snapshot.TotalSpent,
            snapshot.TotalRemaining,
            snapshot.UncategorizedSpent,
            snapshot.Categories
                .Select(category => new BudgetCategorySnapshotResponse(
                    category.CategoryId,
                    category.Name,
                    category.Allocated,
                    category.Spent,
                    category.Remaining))
                .ToArray());

        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static bool TryGetGuidQuery(HttpContext httpContext, string key, out Guid value)
    {
        value = Guid.Empty;
        var raw = httpContext.Request.Query[key].FirstOrDefault();
        return raw is not null && Guid.TryParse(raw, out value);
    }
}

public sealed record CurrentBudgetSnapshotResponse(
    Guid HouseholdId,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    decimal TotalAllocated,
    decimal TotalSpent,
    decimal TotalRemaining,
    decimal UncategorizedSpent,
    BudgetCategorySnapshotResponse[] Categories);

public sealed record BudgetCategorySnapshotResponse(
    Guid CategoryId,
    string Name,
    decimal Allocated,
    decimal Spent,
    decimal Remaining);
