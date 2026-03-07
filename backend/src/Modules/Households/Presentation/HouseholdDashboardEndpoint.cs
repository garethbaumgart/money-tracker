using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Modules.Auth.Application.GetAuthenticatedUser;
using MoneyTracker.Modules.Auth.Domain;
using MoneyTracker.Modules.Budgets.Domain;
using MoneyTracker.Modules.Households.Application.GetHouseholdDashboard;

namespace MoneyTracker.Modules.Households.Presentation;

public static class HouseholdDashboardEndpoint
{
    public static IEndpointRouteBuilder MapHouseholdDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/households/{householdId:guid}/dashboard", GetHouseholdDashboard)
            .WithName("GetHouseholdDashboard")
            .WithSummary("Get the household dashboard.")
            .WithDescription("Returns household-scoped summary totals and recent activity.")
            .Produces<HouseholdDashboardResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task GetHouseholdDashboard(HttpContext httpContext, Guid householdId)
    {
        var authResult = await ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<GetHouseholdDashboardHandler>();
        var result = await handler.HandleAsync(
            new GetHouseholdDashboardQuery(householdId, authResult.AuthenticatedUser!.UserId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                BudgetErrors.BudgetHouseholdNotFound => StatusCodes.Status404NotFound,
                BudgetErrors.BudgetAccessDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };

            await WriteProblemAsync(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status403Forbidden ? "Access denied." : "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                BudgetErrors.ValidationError);
            return;
        }

        var dashboard = result.Dashboard!;
        var response = new HouseholdDashboardResponse(
            dashboard.HouseholdId,
            dashboard.PeriodStartUtc,
            dashboard.PeriodEndUtc,
            dashboard.TotalAllocated,
            dashboard.TotalSpent,
            dashboard.TotalRemaining,
            dashboard.UncategorizedSpent,
            dashboard.Categories
                .Select(category => new HouseholdDashboardCategoryResponse(
                    category.CategoryId,
                    category.Name,
                    category.Allocated,
                    category.Spent,
                    category.Remaining))
                .ToArray(),
            dashboard.RecentTransactions
                .Select(transaction => new HouseholdDashboardTransactionResponse(
                    transaction.Id,
                    transaction.Amount,
                    transaction.OccurredAtUtc,
                    transaction.Description,
                    transaction.CategoryId,
                    transaction.CategoryName))
                .ToArray());

        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static async Task<(bool Success, AuthenticatedUser? AuthenticatedUser, IResult? Problem)> ResolveAuthenticatedUser(HttpContext httpContext)
    {
        var token = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
        var handler = httpContext.RequestServices.GetRequiredService<GetAuthenticatedUserHandler>();
        var authResult = await handler.HandleAsync(new GetAuthenticatedUserQuery(token), httpContext.RequestAborted);

        if (!authResult.IsSuccess)
        {
            var problem = BuildProblemResult(
                StatusCodes.Status401Unauthorized,
                "Authentication required.",
                authResult.ErrorMessage ?? "Authentication required.",
                authResult.ErrorCode ?? AuthErrors.AccessTokenInvalid,
                httpContext.Request.Path);

            return (false, null, problem);
        }

        return (true, new AuthenticatedUser(authResult.UserId, authResult.Email), null);
    }

    private static string? ExtractBearerToken(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            return null;
        }

        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = authorizationHeader.Substring("Bearer ".Length).Trim();
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    private static IResult BuildProblemResult(int statusCode, string title, string detail, string code, string instance)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = instance
        };
        problem.Extensions["code"] = code;
        return TypedResults.Problem(problem);
    }

    private static async Task WriteProblemAsync(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail,
        string? code,
        string fallbackCode)
    {
        await BuildProblemResult(
            statusCode,
            title,
            detail,
            code ?? fallbackCode,
            httpContext.Request.Path).ExecuteAsync(httpContext);
    }
}

public sealed record HouseholdDashboardResponse(
    Guid HouseholdId,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    decimal TotalAllocated,
    decimal TotalSpent,
    decimal TotalRemaining,
    decimal UncategorizedSpent,
    HouseholdDashboardCategoryResponse[] Categories,
    HouseholdDashboardTransactionResponse[] RecentTransactions);

public sealed record HouseholdDashboardCategoryResponse(
    Guid CategoryId,
    string Name,
    decimal Allocated,
    decimal Spent,
    decimal Remaining);

public sealed record HouseholdDashboardTransactionResponse(
    Guid Id,
    decimal Amount,
    DateTimeOffset OccurredAtUtc,
    string? Description,
    Guid? CategoryId,
    string? CategoryName);
