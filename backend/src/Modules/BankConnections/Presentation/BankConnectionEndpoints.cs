using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Modules.Auth.Application.GetAuthenticatedUser;
using MoneyTracker.Modules.Auth.Domain;
using MoneyTracker.Modules.BankConnections.Application.CreateLinkSession;
using MoneyTracker.Modules.BankConnections.Application.GetBankConnections;
using MoneyTracker.Modules.BankConnections.Application.ProcessCallback;
using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Presentation;

public static class BankConnectionEndpoints
{
    public static IServiceCollection AddBankConnectionsModule(this IServiceCollection services)
    {
        services.AddSingleton<IBankConnectionRepository, Infrastructure.InMemoryBankConnectionRepository>();
        services.AddSingleton<IBankProviderAdapter, Infrastructure.InMemoryBankProviderAdapter>();
        services.AddSingleton(TimeProvider.System);
        services.AddHttpClient<Infrastructure.BasiqBankProviderAdapter>(client =>
        {
            client.BaseAddress = new Uri("https://au-api.basiq.io");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<CreateLinkSessionHandler>();
        services.AddScoped<ProcessCallbackHandler>();
        services.AddScoped<GetBankConnectionsHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapBankConnectionEndpoints(this IEndpointRouteBuilder app)
    {
        var createLinkSessionEndpoint = (RouteHandlerBuilder)app.MapPost("/bank/link-session", CreateLinkSession);
        createLinkSessionEndpoint
            .WithName("CreateBankLinkSession")
            .WithSummary("Create a bank link session.")
            .WithDescription("Creates a Basiq consent session and returns a consent URL for the user.")
            .Accepts<CreateLinkSessionRequest>("application/json")
            .Produces<LinkSessionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        var callbackEndpoint = (RouteHandlerBuilder)app.MapPost("/bank/callback", ProcessCallback);
        callbackEndpoint
            .WithName("ProcessBankCallback")
            .WithSummary("Process bank connection callback.")
            .WithDescription("Handles the OAuth redirect callback from the bank provider.")
            .Accepts<ProcessCallbackRequest>("application/json")
            .Produces<BankConnectionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        var getConnectionsEndpoint = (RouteHandlerBuilder)app.MapGet("/bank/connections", GetBankConnections);
        getConnectionsEndpoint
            .WithName("GetBankConnections")
            .WithSummary("List bank connections.")
            .WithDescription("Returns all bank connections for a household with current status.")
            .Produces<BankConnectionsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task CreateLinkSession(HttpContext httpContext)
    {
        var authResult = await ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var (isValidRequest, request, parseProblem) =
            await ReadJsonRequestAsync<CreateLinkSessionRequest>(httpContext);
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
                    BankConnectionErrors.ValidationError,
                    BankConnectionErrors.ValidationError);
            }
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<CreateLinkSessionHandler>();
        var result = await handler.HandleAsync(
            new CreateLinkSessionCommand(
                request.HouseholdId,
                authResult.AuthenticatedUser!.UserId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                BankConnectionErrors.ConnectionHouseholdNotFound => StatusCodes.Status404NotFound,
                BankConnectionErrors.ConnectionAccessDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };

            await WriteProblemAsync(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status403Forbidden ? "Access denied." : "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                BankConnectionErrors.ValidationError);
            return;
        }

        var response = new LinkSessionResponse(result.ConsentUrl!, result.ConnectionId!.Value);
        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static async Task ProcessCallback(HttpContext httpContext)
    {
        var (isValidRequest, request, parseProblem) =
            await ReadJsonRequestAsync<ProcessCallbackRequest>(httpContext);
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
                    BankConnectionErrors.ValidationError,
                    BankConnectionErrors.ValidationError);
            }
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<ProcessCallbackHandler>();
        var result = await handler.HandleAsync(
            new ProcessCallbackCommand(request.ConsentSessionId),
            httpContext.RequestAborted);

        if (!result.IsSuccess && !result.IsFailedConnection)
        {
            var statusCode = result.ErrorCode switch
            {
                BankConnectionErrors.ConnectionNotFound => StatusCodes.Status404NotFound,
                BankConnectionErrors.ConnectionSessionExpired => StatusCodes.Status400BadRequest,
                BankConnectionErrors.ConnectionCallbackInvalid => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status400BadRequest
            };

            await WriteProblemAsync(
                httpContext,
                statusCode,
                "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                BankConnectionErrors.ValidationError);
            return;
        }

        var connection = result.Connection!;
        var response = new BankConnectionResponse(
            connection.Id.Value,
            connection.HouseholdId,
            connection.InstitutionName,
            connection.Status.ToString(),
            connection.ErrorCode,
            connection.ErrorMessage,
            connection.CreatedAtUtc,
            connection.UpdatedAtUtc);

        if (result.IsFailedConnection)
        {
            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Connection failed.",
                result.ErrorMessage ?? "The bank connection could not be established.",
                result.ErrorCode,
                BankConnectionErrors.ConnectionProviderError);
            return;
        }

        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static async Task GetBankConnections(HttpContext httpContext)
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
                BankConnectionErrors.ValidationError,
                BankConnectionErrors.ValidationError);
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<GetBankConnectionsHandler>();
        var result = await handler.HandleAsync(
            new GetBankConnectionsQuery(householdId, authResult.AuthenticatedUser!.UserId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                BankConnectionErrors.ConnectionHouseholdNotFound => StatusCodes.Status404NotFound,
                BankConnectionErrors.ConnectionAccessDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };

            await WriteProblemAsync(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status403Forbidden ? "Access denied." : "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                BankConnectionErrors.ValidationError);
            return;
        }

        var response = new BankConnectionsResponse(
            result.Connections!
                .Select(c => new BankConnectionSummaryResponse(
                    c.Id,
                    c.HouseholdId,
                    c.InstitutionName,
                    c.Status,
                    c.ErrorCode,
                    c.ErrorMessage,
                    c.CreatedAtUtc,
                    c.UpdatedAtUtc))
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
                BankConnectionErrors.ValidationError,
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
                    BankConnectionErrors.ValidationError,
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
                BankConnectionErrors.ValidationError,
                httpContext.Request.Path));
        }
        catch (NotSupportedException)
        {
            return (false, default, BuildProblemResult(
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "The request payload is invalid.",
                BankConnectionErrors.ValidationError,
                httpContext.Request.Path));
        }
        catch (BadHttpRequestException)
        {
            return (false, default, BuildProblemResult(
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "The request payload is invalid.",
                BankConnectionErrors.ValidationError,
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

public sealed record CreateLinkSessionRequest(Guid HouseholdId);

public sealed record ProcessCallbackRequest(string ConsentSessionId);

public sealed record LinkSessionResponse(string ConsentUrl, Guid ConnectionId);

public sealed record BankConnectionResponse(
    Guid Id,
    Guid HouseholdId,
    string? InstitutionName,
    string Status,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record BankConnectionSummaryResponse(
    Guid Id,
    Guid HouseholdId,
    string? InstitutionName,
    string Status,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record BankConnectionsResponse(BankConnectionSummaryResponse[] Connections);

internal sealed record AuthenticatedUser(Guid UserId, string Email);
