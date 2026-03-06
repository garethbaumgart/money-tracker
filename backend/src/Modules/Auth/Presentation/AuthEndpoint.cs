using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Modules.Auth.Application.GetAuthenticatedUser;
using MoneyTracker.Modules.Auth.Application.Logout;
using MoneyTracker.Modules.Auth.Application.RefreshSession;
using MoneyTracker.Modules.Auth.Application.RequestAuthCode;
using MoneyTracker.Modules.Auth.Application.VerifyCode;
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
            .WithDescription("Issues a short-lived, single-use challenge token for sign-in verification.")
            .Accepts<RequestAuthCodeRequest>("application/json")
            .Produces<RequestAuthCodeResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

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

            var response = new VerifyCodeResponse(
                result.Tokens!.UserId,
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
            .WithDescription("Exchanges an email and challenge token for access and refresh tokens.")
            .Accepts<VerifyCodeRequest>("application/json")
            .Produces<VerifyCodeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

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
            .Accepts<RefreshRequest>("application/json")
            .Produces<VerifyCodeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest);

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
            .Accepts<LogoutRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

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
