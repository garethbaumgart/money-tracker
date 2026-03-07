using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Modules.Auth.Application.DeleteUser;
using MoneyTracker.Modules.Auth.Application.ExportUserData;
using MoneyTracker.Modules.Auth.Application.GetAuthenticatedUser;
using MoneyTracker.Modules.Auth.Application.Logout;
using MoneyTracker.Modules.Auth.Application.RefreshSession;
using MoneyTracker.Modules.Auth.Application.RequestAuthCode;
using MoneyTracker.Modules.Auth.Application.VerifyCode;
using MoneyTracker.Modules.Auth.Infrastructure;
using MoneyTracker.Modules.Auth.Domain;

namespace MoneyTracker.Modules.Auth.Presentation;

public static class AuthEndpoint
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services)
    {
        services.AddSingleton<IAuthRepository, InMemoryAuthRepository>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<RequestAuthCodeHandler>();
        services.AddScoped<VerifyCodeHandler>();
        services.AddScoped<RefreshSessionHandler>();
        services.AddScoped<LogoutSessionHandler>();
        services.AddScoped<GetAuthenticatedUserHandler>();
        services.AddScoped<ExportUserDataHandler>();
        services.AddScoped<DeleteUserHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/request-code", async (HttpContext httpContext) =>
        {
            var handler = httpContext.RequestServices.GetRequiredService<RequestAuthCodeHandler>();
            var (isValidRequest, request, parseProblem) = await ReadJsonRequestAsync<RequestAuthCodeRequest>(httpContext);
            if (!isValidRequest)
            {
                await parseProblem!.ExecuteAsync(httpContext);
                return;
            }

            var result = await handler.HandleAsync(
                new RequestAuthCodeCommand(request!.Email),
                httpContext.RequestAborted);

            if (!result.IsSuccess)
            {
                await WriteProblemAsync(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Validation failed.",
                    result.ErrorMessage!,
                    result.ErrorCode!,
                    AuthErrors.ValidationError);
                return;
            }

            var response = new RequestAuthCodeResponse(result.Challenge!.Token, result.Challenge.ExpiresAtUtc);
            var httpResult = TypedResults.Created($"/auth/challenges/{result.Challenge.Token}", response);
            await httpResult.ExecuteAsync(httpContext);
        })
        .WithName("RequestAuthCode")
        .WithSummary("Request a challenge code.")
        .WithDescription("Issues a short-lived, single-use challenge token for sign-in verification.");

        app.MapPost("/auth/verify-code", async (HttpContext httpContext) =>
        {
            var handler = httpContext.RequestServices.GetRequiredService<VerifyCodeHandler>();
            var (isValidRequest, request, parseProblem) = await ReadJsonRequestAsync<VerifyCodeRequest>(httpContext);
            if (!isValidRequest)
            {
                await parseProblem!.ExecuteAsync(httpContext);
                return;
            }

            var result = await handler.HandleAsync(
                new VerifyCodeCommand(request!.Email, request.ChallengeToken),
                httpContext.RequestAborted);

            if (!result.IsSuccess)
            {
                await WriteProblemAsync(
                    httpContext,
                    StatusCodes.Status401Unauthorized,
                    "Verification failed.",
                    result.ErrorMessage!,
                    result.ErrorCode!,
                    AuthErrors.ValidationError);
                return;
            }

            // Fire-and-forget: emit signup_completed activation event via IAnalyticsEventPublisher
            await EmitAnalyticsEventAsync(httpContext, result.Tokens!.UserId, "signup_completed", householdId: null);

            var response = new VerifyCodeResponse(
                result.Tokens.UserId,
                result.Tokens.Email,
                result.Tokens.AccessToken,
                result.Tokens.AccessTokenExpiresAtUtc,
                result.Tokens.RefreshToken,
                result.Tokens.RefreshTokenExpiresAtUtc);
            var httpResult = TypedResults.Ok(response);
            await httpResult.ExecuteAsync(httpContext);
        })
        .WithName("VerifyAuthCode")
        .WithSummary("Verify a challenge code.")
        .WithDescription("Exchanges an email and challenge token for access and refresh tokens.");

        app.MapPost("/auth/refresh", async (HttpContext httpContext) =>
        {
            var handler = httpContext.RequestServices.GetRequiredService<RefreshSessionHandler>();
            var (isValidRequest, request, parseProblem) = await ReadJsonRequestAsync<RefreshRequest>(httpContext);
            if (!isValidRequest)
            {
                await parseProblem!.ExecuteAsync(httpContext);
                return;
            }

            var result = await handler.HandleAsync(
                new RefreshSessionCommand(request!.RefreshToken),
                httpContext.RequestAborted);

            if (!result.IsSuccess)
            {
                await WriteProblemAsync(
                    httpContext,
                    StatusCodes.Status401Unauthorized,
                    "Session refresh failed.",
                    result.ErrorMessage!,
                    result.ErrorCode!,
                    AuthErrors.ValidationError);
                return;
            }

            var response = new VerifyCodeResponse(
                result.Tokens!.UserId,
                result.Tokens.Email,
                result.Tokens.AccessToken,
                result.Tokens.AccessTokenExpiresAtUtc,
                result.Tokens.RefreshToken,
                result.Tokens.RefreshTokenExpiresAtUtc);
            await TypedResults.Ok(response).ExecuteAsync(httpContext);
        })
        .WithName("RefreshAuthSession")
        .WithSummary("Refresh an authentication session.")
        .WithDescription("Uses a refresh token to rotate access and refresh credentials.")
        .WithDescription("Uses a refresh token to rotate access and refresh credentials.");

        app.MapPost("/auth/logout", async (HttpContext httpContext) =>
        {
            var handler = httpContext.RequestServices.GetRequiredService<LogoutSessionHandler>();
            var (isValidRequest, request, parseProblem) = await ReadJsonRequestAsync<LogoutRequest>(httpContext);
            if (!isValidRequest)
            {
                await parseProblem!.ExecuteAsync(httpContext);
                return;
            }

            await handler.HandleAsync(new LogoutSessionCommand(request!.RefreshToken), httpContext.RequestAborted);

            await TypedResults.NoContent().ExecuteAsync(httpContext);
        })
        .WithName("Logout")
        .WithSummary("Logout an authenticated session.")
        .WithDescription("Revokes a refresh token and associated access token.")
        .WithDescription("Revokes a refresh token and associated access token.");

        app.MapGet("/users/{id:guid}/data-export", async (HttpContext httpContext, Guid id) =>
        {
            var authHandler = httpContext.RequestServices.GetRequiredService<GetAuthenticatedUserHandler>();
            var token = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
            var authResult = await authHandler.HandleAsync(new GetAuthenticatedUserQuery(token), httpContext.RequestAborted);
            if (!authResult.IsSuccess)
            {
                await WriteProblemAsync(
                    httpContext,
                    StatusCodes.Status401Unauthorized,
                    "Authentication required.",
                    authResult.ErrorMessage ?? "Authentication required.",
                    authResult.ErrorCode ?? AuthErrors.AccessTokenInvalid,
                    AuthErrors.AccessTokenInvalid);
                return;
            }

            if (authResult.UserId != id)
            {
                await WriteProblemAsync(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Access denied.",
                    "You can only export your own data.",
                    AuthErrors.DataExportForbidden,
                    AuthErrors.DataExportForbidden);
                return;
            }

            var handler = httpContext.RequestServices.GetRequiredService<ExportUserDataHandler>();
            var participants = ResolveExportParticipants(httpContext);
            var result = await handler.HandleAsync(
                new ExportUserDataQuery(id),
                participants,
                httpContext.RequestAborted);

            if (!result.IsSuccess)
            {
                await WriteProblemAsync(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Export failed.",
                    result.ErrorMessage!,
                    result.ErrorCode!,
                    AuthErrors.ValidationError);
                return;
            }

            await TypedResults.Ok(result.Data).ExecuteAsync(httpContext);
        })
        .WithName("ExportUserData")
        .WithSummary("Export user data.")
        .WithDescription("Returns all data associated with the authenticated user across all modules.");

        app.MapDelete("/users/{id:guid}", async (HttpContext httpContext, Guid id) =>
        {
            var authHandler = httpContext.RequestServices.GetRequiredService<GetAuthenticatedUserHandler>();
            var token = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
            var authResult = await authHandler.HandleAsync(new GetAuthenticatedUserQuery(token), httpContext.RequestAborted);
            if (!authResult.IsSuccess)
            {
                await WriteProblemAsync(
                    httpContext,
                    StatusCodes.Status401Unauthorized,
                    "Authentication required.",
                    authResult.ErrorMessage ?? "Authentication required.",
                    authResult.ErrorCode ?? AuthErrors.AccessTokenInvalid,
                    AuthErrors.AccessTokenInvalid);
                return;
            }

            if (authResult.UserId != id)
            {
                await WriteProblemAsync(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Access denied.",
                    "You can only delete your own account.",
                    AuthErrors.DeleteForbidden,
                    AuthErrors.DeleteForbidden);
                return;
            }

            var handler = httpContext.RequestServices.GetRequiredService<DeleteUserHandler>();
            var deletionParticipants = ResolveDeletionParticipants(httpContext);
            var result = await handler.HandleAsync(
                new DeleteUserCommand(id),
                deletionParticipants,
                httpContext.RequestAborted);

            if (!result.IsSuccess)
            {
                await WriteProblemAsync(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Deletion failed.",
                    result.ErrorMessage!,
                    result.ErrorCode!,
                    AuthErrors.ValidationError);
                return;
            }

            var response = new DeleteUserResponse(result.ScheduledPurgeAtUtc!.Value);
            httpContext.Response.StatusCode = StatusCodes.Status202Accepted;
            await httpContext.Response.WriteAsJsonAsync(response, httpContext.RequestAborted);
        })
        .WithName("DeleteUser")
        .WithSummary("Delete user account.")
        .WithDescription("Soft-deletes the authenticated user and schedules a full data purge after 30 days.");

        return app;
    }

    private static async Task<(bool IsValid, TRequest? Request, IResult? Error)> ReadJsonRequestAsync<TRequest>(HttpContext httpContext)
        where TRequest : class
    {
        var contentType = httpContext.Request.ContentType;
        if (string.IsNullOrWhiteSpace(contentType) || !IsJsonContentType(contentType))
        {
            return (false, default, BuildValidationProblem(
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "The request payload is required to be JSON.",
                AuthErrors.ValidationError,
                httpContext.Request.Path));
        }

        try
        {
            var request = await httpContext.Request.ReadFromJsonAsync<TRequest>(cancellationToken: httpContext.RequestAborted);
            if (request is null)
            {
                return (false, default, BuildValidationProblem(
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "The request payload is invalid.",
                AuthErrors.ValidationError,
                httpContext.Request.Path));
            }

            return (true, request, null);
        }
        catch (JsonException)
        {
            return (false, default, BuildValidationProblem(
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "The request payload is invalid.",
                AuthErrors.ValidationError,
                httpContext.Request.Path));
        }
        catch (NotSupportedException)
        {
            return (false, default, BuildValidationProblem(
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "The request payload is invalid.",
                AuthErrors.ValidationError,
                httpContext.Request.Path));
        }
        catch (BadHttpRequestException)
        {
            return (false, default, BuildValidationProblem(
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "The request payload is invalid.",
                AuthErrors.ValidationError,
                httpContext.Request.Path));
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail,
        string code,
        string fallbackCode)
    {
        var result = BuildProblem(statusCode, title, detail, code ?? fallbackCode, httpContext.Request.Path);
        await TypedResults.Problem(result).ExecuteAsync(httpContext);
    }

    private static IResult BuildValidationProblem(int statusCode, string title, string detail, string code, string? instance)
    {
        return TypedResults.Problem(BuildProblem(statusCode, title, detail, code, instance));
    }

    private static ProblemDetails BuildProblem(int statusCode, string title, string detail, string code, string? instance)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = instance
        };
        problem.Extensions["code"] = code;
        return problem;
    }

    private static bool IsJsonContentType(string contentType)
    {
        var mediaType = contentType.Split(';')[0].Trim();
        return mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractBearerToken(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
            return null;
        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;
        var token = authorizationHeader.Substring("Bearer ".Length).Trim();
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    /// <summary>
    /// Resolves IUserDataExportParticipant implementations from DI at runtime
    /// to avoid circular project reference (SharedKernel references Auth).
    /// </summary>
    private static IReadOnlyList<ExportParticipantEntry> ResolveExportParticipants(HttpContext httpContext)
    {
        try
        {
            var participantType = Type.GetType(
                "MoneyTracker.Modules.SharedKernel.Privacy.IUserDataExportParticipant, MoneyTracker.Modules.SharedKernel");
            if (participantType is null) return [];

            var enumerableType = typeof(IEnumerable<>).MakeGenericType(participantType);
            var participants = httpContext.RequestServices.GetService(enumerableType);
            if (participants is null) return [];

            var exportMethod = participantType.GetMethod("ExportUserDataAsync");
            if (exportMethod is null) return [];

            var entries = new List<ExportParticipantEntry>();
            foreach (var participant in (System.Collections.IEnumerable)participants)
            {
                var name = participant.GetType().Name.Replace("DataExportParticipant", "");
                entries.Add(new ExportParticipantEntry(name, async (userId, ct) =>
                {
                    var task = (Task<object>?)exportMethod.Invoke(participant, [userId, ct]);
                    return task is not null ? await task : new object();
                }));
            }

            return entries;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Resolves IUserDeletionParticipant implementations from DI at runtime
    /// to avoid circular project reference (SharedKernel references Auth).
    /// </summary>
    private static IReadOnlyList<Func<Guid, CancellationToken, Task>> ResolveDeletionParticipants(HttpContext httpContext)
    {
        try
        {
            var participantType = Type.GetType(
                "MoneyTracker.Modules.SharedKernel.Privacy.IUserDeletionParticipant, MoneyTracker.Modules.SharedKernel");
            if (participantType is null) return [];

            var enumerableType = typeof(IEnumerable<>).MakeGenericType(participantType);
            var participants = httpContext.RequestServices.GetService(enumerableType);
            if (participants is null) return [];

            var deleteMethod = participantType.GetMethod("DeleteUserDataAsync");
            if (deleteMethod is null) return [];

            var entries = new List<Func<Guid, CancellationToken, Task>>();
            foreach (var participant in (System.Collections.IEnumerable)participants)
            {
                var p = participant;
                entries.Add(async (userId, ct) =>
                {
                    var task = (Task?)deleteMethod.Invoke(p, [userId, ct]);
                    if (task is not null) await task;
                });
            }

            return entries;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Resolves IAnalyticsEventPublisher from DI at runtime to avoid circular project reference
    /// (SharedKernel references Auth, so Auth cannot reference SharedKernel).
    /// Fire-and-forget: failures are silently swallowed.
    /// </summary>
    private static async Task EmitAnalyticsEventAsync(
        HttpContext httpContext, Guid userId, string milestone, Guid? householdId)
    {
        try
        {
            // Resolve by assembly-qualified type name to avoid compile-time dependency
            var publisherType = Type.GetType(
                "MoneyTracker.Modules.SharedKernel.Analytics.IAnalyticsEventPublisher, MoneyTracker.Modules.SharedKernel");
            if (publisherType is null) return;

            var publisher = httpContext.RequestServices.GetService(publisherType);
            if (publisher is null) return;

            var publishMethod = publisherType.GetMethod("PublishAsync");
            if (publishMethod is null) return;

            var task = (Task?)publishMethod.Invoke(publisher,
                [userId, milestone, householdId, httpContext.RequestAborted]);
            if (task is not null) await task;
        }
        catch
        {
            // Analytics emission must not fail the auth flow.
        }
    }
}

public sealed record RequestAuthCodeRequest(string Email);

public sealed record RequestAuthCodeResponse(string ChallengeToken, DateTimeOffset ExpiresAtUtc);

public sealed record VerifyCodeRequest(string Email, string ChallengeToken);

public sealed record VerifyCodeResponse(
    Guid UserId,
    string Email,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record DeleteUserResponse(DateTimeOffset ScheduledPurgeAtUtc);
