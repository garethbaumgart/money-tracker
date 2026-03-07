using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Modules.Auth.Application.GetAuthenticatedUser;
using MoneyTracker.Modules.Auth.Domain;
using MoneyTracker.Modules.Transactions.Application.CreateTransaction;
using MoneyTracker.Modules.Transactions.Application.GetTransactions;
using MoneyTracker.Modules.Transactions.Domain;

namespace MoneyTracker.Modules.Transactions.Presentation;

public static class TransactionEndpoints
{
    public static IServiceCollection AddTransactionsModule(this IServiceCollection services)
    {
        services.AddSingleton<ITransactionRepository, Infrastructure.InMemoryTransactionRepository>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CreateTransactionHandler>();
        services.AddScoped<GetTransactionsHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var createTransactionEndpoint = (RouteHandlerBuilder)app.MapPost("/transactions", CreateTransaction);
        createTransactionEndpoint
            .WithName("CreateTransaction")
            .WithSummary("Create a manual transaction.")
            .WithDescription("Creates a household-scoped manual transaction entry.")
            .Accepts<CreateTransactionRequest>("application/json")
            .Produces<TransactionResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapGet("/transactions", GetTransactions)
            .WithName("GetTransactions")
            .WithSummary("List transactions.")
            .WithDescription("Returns household-scoped manual transactions.");

        return app;
    }

    private static async Task CreateTransaction(HttpContext httpContext)
    {
        var authResult = await ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var (isValidRequest, request, parseProblem) =
            await ReadJsonRequestAsync<CreateTransactionRequest>(httpContext);
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
                    TransactionErrors.ValidationError,
                    TransactionErrors.ValidationError);
            }
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<CreateTransactionHandler>();
        var result = await handler.HandleAsync(
            new CreateTransactionCommand(
                request.HouseholdId,
                request.Amount,
                request.OccurredAtUtc,
                request.Description,
                request.CategoryId,
                authResult.AuthenticatedUser!.UserId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                TransactionErrors.TransactionHouseholdNotFound => StatusCodes.Status404NotFound,
                TransactionErrors.TransactionAccessDenied => StatusCodes.Status403Forbidden,
                TransactionErrors.TransactionCategoryNotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status400BadRequest
            };

            await WriteProblemAsync(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status403Forbidden ? "Access denied." : "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                TransactionErrors.ValidationError);
            return;
        }

        var transaction = result.Transaction!;
        var response = new TransactionResponse(
            transaction.Id.Value,
            transaction.HouseholdId,
            transaction.Amount,
            transaction.OccurredAtUtc,
            transaction.Description,
            transaction.CategoryId,
            transaction.CreatedAtUtc);
        await TypedResults.Created($"/transactions/{transaction.Id.Value}", response)
            .ExecuteAsync(httpContext);
    }

    private static async Task GetTransactions(HttpContext httpContext)
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
                TransactionErrors.ValidationError,
                TransactionErrors.ValidationError);
            return;
        }

        var (fromUtc, fromValid) = TryGetDateTimeOffsetQuery(httpContext, "fromUtc");
        var (toUtc, toValid) = TryGetDateTimeOffsetQuery(httpContext, "toUtc");
        if (!fromValid || !toValid)
        {
            var message = !fromValid
                ? "fromUtc query parameter is invalid."
                : "toUtc query parameter is invalid.";
            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                message,
                TransactionErrors.ValidationError,
                TransactionErrors.ValidationError);
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<GetTransactionsHandler>();
        var result = await handler.HandleAsync(
            new GetTransactionsQuery(householdId, authResult.AuthenticatedUser!.UserId, fromUtc, toUtc),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                TransactionErrors.TransactionHouseholdNotFound => StatusCodes.Status404NotFound,
                TransactionErrors.TransactionAccessDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };

            await WriteProblemAsync(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status403Forbidden ? "Access denied." : "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                TransactionErrors.ValidationError);
            return;
        }

        var response = new TransactionsResponse(
            result.Transactions!
                .Select(transaction => new TransactionSummaryResponse(
                    transaction.Id,
                    transaction.Amount,
                    transaction.OccurredAtUtc,
                    transaction.Description,
                    transaction.CategoryId,
                    transaction.CategoryName,
                    transaction.CreatedAtUtc,
                    transaction.Source,
                    transaction.ExternalTransactionId,
                    transaction.BankConnectionId))
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
                TransactionErrors.ValidationError,
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
                    TransactionErrors.ValidationError,
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
                TransactionErrors.ValidationError,
                httpContext.Request.Path));
        }
        catch (NotSupportedException)
        {
            return (false, default, BuildProblemResult(
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "The request payload is invalid.",
                TransactionErrors.ValidationError,
                httpContext.Request.Path));
        }
        catch (BadHttpRequestException)
        {
            return (false, default, BuildProblemResult(
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "The request payload is invalid.",
                TransactionErrors.ValidationError,
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

    private static (DateTimeOffset? Value, bool IsValid) TryGetDateTimeOffsetQuery(HttpContext httpContext, string key)
    {
        var raw = httpContext.Request.Query[key].FirstOrDefault();
        if (raw is null)
        {
            return (null, true);
        }

        if (DateTimeOffset.TryParse(raw, out var value))
        {
            return (value, true);
        }

        return (null, false);
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

public sealed record CreateTransactionRequest(
    Guid HouseholdId,
    decimal Amount,
    DateTimeOffset OccurredAtUtc,
    string? Description,
    Guid? CategoryId);

public sealed record TransactionResponse(
    Guid Id,
    Guid HouseholdId,
    decimal Amount,
    DateTimeOffset OccurredAtUtc,
    string? Description,
    Guid? CategoryId,
    DateTimeOffset CreatedAtUtc);

public sealed record TransactionSummaryResponse(
    Guid Id,
    decimal Amount,
    DateTimeOffset OccurredAtUtc,
    string? Description,
    Guid? CategoryId,
    string? CategoryName,
    DateTimeOffset CreatedAtUtc,
    string Source,
    string? ExternalTransactionId,
    Guid? BankConnectionId);

public sealed record TransactionsResponse(TransactionSummaryResponse[] Transactions);

internal sealed record AuthenticatedUser(Guid UserId, string Email);
