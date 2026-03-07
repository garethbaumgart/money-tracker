using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Modules.Auth.Application.GetAuthenticatedUser;
using MoneyTracker.Modules.Auth.Domain;
using MoneyTracker.Modules.Budgets.Application.CreateBudgetCategory;
using MoneyTracker.Modules.Budgets.Application.GetBudgetCategories;
using MoneyTracker.Modules.Budgets.Application.UpsertBudgetAllocation;
using MoneyTracker.Modules.Budgets.Domain;
using MoneyTracker.Modules.SharedKernel.Privacy;

namespace MoneyTracker.Modules.Budgets.Presentation;

public static class BudgetEndpoints
{
    public static IServiceCollection AddBudgetsModule(this IServiceCollection services)
    {
        services.AddSingleton<IBudgetRepository, Infrastructure.InMemoryBudgetRepository>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CreateBudgetCategoryHandler>();
        services.AddScoped<GetBudgetCategoriesHandler>();
        services.AddScoped<UpsertBudgetAllocationHandler>();
        services.AddSingleton<IUserDataExportParticipant, Infrastructure.BudgetDataExportParticipant>();
        services.AddSingleton<IUserDeletionParticipant, Infrastructure.BudgetDataExportParticipant>();

        return services;
    }

    public static IEndpointRouteBuilder MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        var createCategoryEndpoint = (RouteHandlerBuilder)app.MapPost("/budgets/categories", CreateBudgetCategory);
        createCategoryEndpoint
            .WithName("CreateBudgetCategory")
            .WithSummary("Create a budget category.")
            .WithDescription("Creates a household-scoped budget category.")
            .Accepts<CreateBudgetCategoryRequest>("application/json")
            .Produces<BudgetCategoryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        app.MapGet("/budgets/categories", GetBudgetCategories)
            .WithName("GetBudgetCategories")
            .WithSummary("List budget categories.")
            .WithDescription("Returns household-scoped budget categories.");

        var upsertAllocationEndpoint = (RouteHandlerBuilder)app.MapPost("/budgets", UpsertBudgetAllocation);
        upsertAllocationEndpoint
            .WithName("UpsertBudgetAllocation")
            .WithSummary("Create or update a budget allocation.")
            .WithDescription("Creates or updates a monthly budget allocation for a category.")
            .Accepts<UpsertBudgetAllocationRequest>("application/json")
            .Produces<BudgetAllocationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task CreateBudgetCategory(HttpContext httpContext)
    {
        var authResult = await ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var (isValidRequest, request, parseProblem) =
            await ReadJsonRequestAsync<CreateBudgetCategoryRequest>(httpContext);
        if (!isValidRequest || request is null)
        {
            if (parseProblem is not null)
            {
                await parseProblem.ExecuteAsync(httpContext);
            }
            else
            {
                await WriteProblemAsync(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Validation failed.",
                    "The request payload is required.",
                    BudgetErrors.ValidationError,
                    BudgetErrors.ValidationError);
            }
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<CreateBudgetCategoryHandler>();
        var result = await handler.HandleAsync(
            new CreateBudgetCategoryCommand(
                request.HouseholdId,
                request.Name,
                authResult.AuthenticatedUser!.UserId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                BudgetErrors.BudgetHouseholdNotFound => StatusCodes.Status404NotFound,
                BudgetErrors.BudgetAccessDenied => StatusCodes.Status403Forbidden,
                BudgetErrors.BudgetCategoryNameConflict => StatusCodes.Status409Conflict,
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

        var category = result.Category!;
        var response = new BudgetCategoryResponse(category.Id.Value, category.Name, category.CreatedAtUtc);
        await TypedResults.Created($"/budgets/categories/{category.Id.Value}", response)
            .ExecuteAsync(httpContext);
    }

    private static async Task GetBudgetCategories(HttpContext httpContext)
    {
        var authResult = await ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        if (!TryGetGuidQuery(httpContext, "householdId", out var householdId))
        {
            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "householdId query parameter is required.",
                BudgetErrors.ValidationError,
                BudgetErrors.ValidationError);
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<GetBudgetCategoriesHandler>();
        var result = await handler.HandleAsync(
            new GetBudgetCategoriesQuery(householdId, authResult.AuthenticatedUser!.UserId),
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

        var response = new BudgetCategoriesResponse(
            result.Categories!
                .Select(category => new BudgetCategoryResponse(
                    category.Id.Value,
                    category.Name,
                    category.CreatedAtUtc))
                .ToArray());
        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static async Task UpsertBudgetAllocation(HttpContext httpContext)
    {
        var authResult = await ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var (isValidRequest, request, parseProblem) =
            await ReadJsonRequestAsync<UpsertBudgetAllocationRequest>(httpContext);
        if (!isValidRequest || request is null)
        {
            if (parseProblem is not null)
            {
                await parseProblem.ExecuteAsync(httpContext);
            }
            else
            {
                await WriteProblemAsync(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Validation failed.",
                    "The request payload is required.",
                    BudgetErrors.ValidationError,
                    BudgetErrors.ValidationError);
            }
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<UpsertBudgetAllocationHandler>();
        var result = await handler.HandleAsync(
            new UpsertBudgetAllocationCommand(
                request.HouseholdId,
                request.CategoryId,
                request.Amount,
                request.PeriodStartUtc,
                authResult.AuthenticatedUser!.UserId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                BudgetErrors.BudgetHouseholdNotFound => StatusCodes.Status404NotFound,
                BudgetErrors.BudgetAccessDenied => StatusCodes.Status403Forbidden,
                BudgetErrors.BudgetCategoryNotFound => StatusCodes.Status404NotFound,
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

        var allocation = result.Allocation!;
        var response = new BudgetAllocationResponse(
            allocation.Id.Value,
            allocation.CategoryId.Value,
            allocation.Amount,
            allocation.PeriodStartUtc,
            allocation.CreatedAtUtc,
            allocation.UpdatedAtUtc);
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

    private static async Task<(bool IsValid, TRequest? Request, IResult? Error)> ReadJsonRequestAsync<TRequest>(HttpContext httpContext)
        where TRequest : class
    {
        var contentType = httpContext.Request.ContentType;
        if (string.IsNullOrWhiteSpace(contentType) || !IsJsonContentType(contentType))
        {
            return (false, default, BuildProblemResult(
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "The request payload is required to be JSON.",
                BudgetErrors.ValidationError,
                httpContext.Request.Path));
        }

        try
        {
            var request = await httpContext.Request.ReadFromJsonAsync<TRequest>(cancellationToken: httpContext.RequestAborted);
            if (request is null)
            {
                return (false, default, BuildProblemResult(
                    StatusCodes.Status400BadRequest,
                    "Validation failed.",
                    "The request payload is invalid.",
                    BudgetErrors.ValidationError,
                    httpContext.Request.Path));
            }

            return (true, request, null);
        }
        catch (JsonException)
        {
            return (false, default, BuildProblemResult(
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "The request payload is invalid.",
                BudgetErrors.ValidationError,
                httpContext.Request.Path));
        }
        catch (NotSupportedException)
        {
            return (false, default, BuildProblemResult(
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "The request payload is invalid.",
                BudgetErrors.ValidationError,
                httpContext.Request.Path));
        }
        catch (BadHttpRequestException)
        {
            return (false, default, BuildProblemResult(
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "The request payload is invalid.",
                BudgetErrors.ValidationError,
                httpContext.Request.Path));
        }
    }

    private static bool IsJsonContentType(string contentType)
    {
        var mediaType = contentType.Split(';')[0].Trim();
        return mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetGuidQuery(HttpContext httpContext, string key, out Guid value)
    {
        value = Guid.Empty;
        var raw = httpContext.Request.Query[key].FirstOrDefault();
        return raw is not null && Guid.TryParse(raw, out value);
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

public sealed record CreateBudgetCategoryRequest(Guid HouseholdId, string Name);

public sealed record BudgetCategoryResponse(Guid Id, string Name, DateTimeOffset CreatedAtUtc);

public sealed record BudgetCategoriesResponse(BudgetCategoryResponse[] Categories);

public sealed record UpsertBudgetAllocationRequest(
    Guid HouseholdId,
    Guid CategoryId,
    decimal Amount,
    DateTimeOffset? PeriodStartUtc);

public sealed record BudgetAllocationResponse(
    Guid Id,
    Guid CategoryId,
    decimal Amount,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

internal sealed record AuthenticatedUser(Guid UserId, string Email);
