using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Modules.BankConnections.Application.CreateLinkSession;
using MoneyTracker.Modules.BankConnections.Application.GetBankConnections;
using MoneyTracker.Modules.BankConnections.Application.ProcessCallback;
using MoneyTracker.Modules.BankConnections.Domain;
using MoneyTracker.Modules.SharedKernel.Households;
using MoneyTracker.Modules.SharedKernel.Presentation;

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
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
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
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var (isValidRequest, request, parseProblem) =
            await EndpointHelpers.ReadJsonRequestAsync<CreateLinkSessionRequest>(httpContext, BankConnectionErrors.ValidationError);
        if (!isValidRequest || request is null)
        {
            if (parseProblem is not null)
            {
                await parseProblem.ExecuteAsync(httpContext);
            }
            else
            {
                await EndpointHelpers.WriteProblemAsync(
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

            await EndpointHelpers.WriteProblemAsync(
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
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var (isValidRequest, request, parseProblem) =
            await EndpointHelpers.ReadJsonRequestAsync<ProcessCallbackRequest>(httpContext, BankConnectionErrors.ValidationError);
        if (!isValidRequest || request is null)
        {
            if (parseProblem is not null)
            {
                await parseProblem.ExecuteAsync(httpContext);
            }
            else
            {
                await EndpointHelpers.WriteProblemAsync(
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

            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
                "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                BankConnectionErrors.ValidationError);
            return;
        }

        var connection = result.Connection!;

        // Verify the authenticated user has access to the connection's household
        var householdAccess = httpContext.RequestServices.GetRequiredService<IHouseholdAccessService>();
        var accessResult = await householdAccess.CheckMemberAsync(
            connection.HouseholdId,
            authResult.AuthenticatedUser!.UserId,
            httpContext.RequestAborted);

        if (!accessResult.IsMember)
        {
            var statusCode = accessResult.HouseholdExists
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status404NotFound;

            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status403Forbidden ? "Access denied." : "Not found.",
                statusCode == StatusCodes.Status403Forbidden
                    ? "You do not have access to this household."
                    : "The household was not found.",
                statusCode == StatusCodes.Status403Forbidden
                    ? BankConnectionErrors.ConnectionAccessDenied
                    : BankConnectionErrors.ConnectionHouseholdNotFound,
                BankConnectionErrors.ValidationError);
            return;
        }

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
            await EndpointHelpers.WriteProblemAsync(
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
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        if (!TryGetGuidQuery(httpContext, "householdId", out var householdId))
        {
            await EndpointHelpers.WriteProblemAsync(
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

            await EndpointHelpers.WriteProblemAsync(
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

    private static bool TryGetGuidQuery(HttpContext httpContext, string key, out Guid value)
    {
        value = Guid.Empty;
        var raw = httpContext.Request.Query[key].FirstOrDefault();
        return raw is not null && Guid.TryParse(raw, out value);
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
