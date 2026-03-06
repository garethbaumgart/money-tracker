using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Modules.Auth.Application.GetAuthenticatedUser;
using MoneyTracker.Modules.Auth.Domain;
using MoneyTracker.Modules.Notifications.Application.RegisterDeviceToken;
using MoneyTracker.Modules.Notifications.Domain;

namespace MoneyTracker.Modules.Notifications.Presentation;

public static class NotificationEndpoints
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        services.AddSingleton<INotificationTokenRepository, Infrastructure.InMemoryNotificationTokenRepository>();
        services.AddSingleton<INotificationSender, Infrastructure.InMemoryNotificationSender>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<RegisterDeviceTokenHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var registerDeviceEndpoint = (RouteHandlerBuilder)app.MapPost(
            "/notifications/device-tokens",
            RegisterDeviceToken);
        registerDeviceEndpoint
            .WithName("RegisterDeviceToken")
            .WithSummary("Register a device token.")
            .WithDescription("Registers or updates a push notification device token.")
            .Accepts<RegisterDeviceTokenRequest>("application/json")
            .Produces<RegisterDeviceTokenResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task RegisterDeviceToken(HttpContext httpContext)
    {
        var authResult = await ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var (isValidRequest, request, parseProblem) =
            await ReadJsonRequestAsync<RegisterDeviceTokenRequest>(httpContext);
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
                    NotificationErrors.ValidationError,
                    NotificationErrors.ValidationError);
            }
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<RegisterDeviceTokenHandler>();
        var result = await handler.HandleAsync(
            new RegisterDeviceTokenCommand(
                request.DeviceId,
                request.Token,
                request.Platform,
                authResult.AuthenticatedUser!.UserId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                NotificationErrors.ValidationError);
            return;
        }

        var token = result.Token!;
        var response = new RegisterDeviceTokenResponse(
            token.DeviceId,
            token.Token,
            token.Platform,
            token.RegisteredAtUtc);
        await TypedResults.Created($"/notifications/device-tokens/{token.DeviceId}", response)
            .ExecuteAsync(httpContext);
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
                NotificationErrors.ValidationError,
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
                    NotificationErrors.ValidationError,
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
                NotificationErrors.ValidationError,
                httpContext.Request.Path));
        }
        catch (NotSupportedException)
        {
            return (false, default, BuildProblemResult(
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "The request payload is invalid.",
                NotificationErrors.ValidationError,
                httpContext.Request.Path));
        }
        catch (BadHttpRequestException)
        {
            return (false, default, BuildProblemResult(
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "The request payload is invalid.",
                NotificationErrors.ValidationError,
                httpContext.Request.Path));
        }
    }

    private static bool IsJsonContentType(string contentType)
    {
        var mediaType = contentType.Split(';')[0].Trim();
        return mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase);
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

public sealed record RegisterDeviceTokenRequest(string DeviceId, string Token, string Platform);

public sealed record RegisterDeviceTokenResponse(
    string DeviceId,
    string Token,
    string Platform,
    DateTimeOffset RegisteredAtUtc);

internal sealed record AuthenticatedUser(Guid UserId, string Email);
