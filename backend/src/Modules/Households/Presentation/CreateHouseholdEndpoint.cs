using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Modules.Auth.Application.GetAuthenticatedUser;
using MoneyTracker.Modules.Auth.Domain;
using MoneyTracker.Modules.Households.Application.AcceptHouseholdInvitation;
using MoneyTracker.Modules.Households.Application.CreateHousehold;
using MoneyTracker.Modules.Households.Application.GetHouseholdMembers;
using MoneyTracker.Modules.Households.Application.InviteHouseholdMember;
using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Modules.Households.Presentation;

public static class CreateHouseholdEndpoint
{
    public static IServiceCollection AddHouseholdsModule(this IServiceCollection services)
    {
        services.AddSingleton<IHouseholdRepository, Infrastructure.InMemoryHouseholdRepository>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CreateHouseholdHandler>();
        services.AddScoped<InviteHouseholdMemberHandler>();
        services.AddScoped<AcceptHouseholdInvitationHandler>();
        services.AddScoped<GetHouseholdMembersHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapHouseholdEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/households", CreateHousehold)
            .WithName("CreateHousehold")
            .WithSummary("Create a household.")
            .WithDescription("Creates a household owned by the authenticated user.")
            .Accepts<CreateHouseholdRequest>("application/json")
            .Produces<CreateHouseholdResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);

        app.MapPost("/households/{householdId:guid}/invite", InviteHouseholdMember)
            .WithName("InviteHouseholdMember")
            .WithSummary("Invite a user to a household.")
            .WithDescription("Only the household owner may issue an invitation token.")
            .Accepts<InviteHouseholdMemberRequest>("application/json")
            .Produces<InviteHouseholdMemberResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapPost("/households/invitations/{invitationToken}/accept", AcceptHouseholdInvitation)
            .WithName("AcceptHouseholdInvitation")
            .WithSummary("Accept a household invitation.")
            .WithDescription("Adds the authenticated user as a household member.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        app.MapGet("/households/{householdId:guid}/members", GetHouseholdMembers)
            .WithName("GetHouseholdMembers")
            .WithSummary("Get household members.")
            .WithDescription("Returns all members for an existing household.")
            .Produces<GetHouseholdMembersResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task CreateHousehold(HttpContext httpContext)
    {
        var authResult = await ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var authUser = authResult.Value.AuthenticatedUser;
        var (isValidRequest, request, parseProblem) = await ReadJsonRequestAsync<CreateHouseholdRequest>(httpContext);
        if (!isValidRequest)
        {
            await parseProblem!.ExecuteAsync(httpContext);
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<CreateHouseholdHandler>();
        var result = await handler.HandleAsync(
            new CreateHouseholdCommand(request!.Name, authUser.UserId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                result.ErrorMessage!,
                result.ErrorCode,
                HouseholdErrors.ValidationError);
            return;
        }

        var household = result.Household!;
        var response = new CreateHouseholdResponse(household.Id.Value, household.Name, household.CreatedAtUtc);
        await TypedResults.Created($"/households/{household.Id.Value}", response).ExecuteAsync(httpContext);
    }

    private static async Task InviteHouseholdMember(HttpContext httpContext, Guid householdId)
    {
        var authResult = await ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var authUser = authResult.Value.AuthenticatedUser;
        var (isValidRequest, request, parseProblem) = await ReadJsonRequestAsync<InviteHouseholdMemberRequest>(httpContext);
        if (!isValidRequest)
        {
            await parseProblem!.ExecuteAsync(httpContext);
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<InviteHouseholdMemberHandler>();
        var result = await handler.HandleAsync(
            new InviteHouseholdMemberCommand(
                new HouseholdId(householdId),
                authUser.UserId,
                request!.InviteeEmail),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                HouseholdErrors.HouseholdNotFound => StatusCodes.Status404NotFound,
                HouseholdErrors.HouseholdAccessDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };

            await WriteProblemAsync(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status403Forbidden ? "Access denied." : "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode!,
                HouseholdErrors.ValidationError);
            return;
        }

        var response = new InviteHouseholdMemberResponse(result.InvitationToken!, result.InvitationExpiresAtUtc!.Value);
        await TypedResults.Created($"/households/{householdId}/invitations/{result.InvitationToken}", response)
            .ExecuteAsync(httpContext);
    }

    private static async Task AcceptHouseholdInvitation(HttpContext httpContext, string invitationToken)
    {
        var authResult = await ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var authUser = authResult.Value.AuthenticatedUser;
        var handler = httpContext.RequestServices.GetRequiredService<AcceptHouseholdInvitationHandler>();
        var result = await handler.HandleAsync(
            new AcceptHouseholdInvitationCommand(invitationToken, authUser.UserId, authUser.Email),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                HouseholdErrors.HouseholdInvitationNotFound => StatusCodes.Status404NotFound,
                HouseholdErrors.HouseholdInvitationEmailMismatch => StatusCodes.Status403Forbidden,
                HouseholdErrors.HouseholdInvitationExpired => StatusCodes.Status409Conflict,
                HouseholdErrors.HouseholdInvitationUsed => StatusCodes.Status409Conflict,
                HouseholdErrors.HouseholdNotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status409Conflict
            };

            var title = statusCode == StatusCodes.Status403Forbidden
                ? "Invitation rejected."
                : "Invitation could not be accepted.";

            await WriteProblemAsync(
                httpContext,
                statusCode,
                title,
                result.ErrorMessage!,
                result.ErrorCode!,
                HouseholdErrors.ValidationError);
            return;
        }

        await TypedResults.Ok().ExecuteAsync(httpContext);
    }

    private static async Task GetHouseholdMembers(HttpContext httpContext, Guid householdId)
    {
        var authResult = await ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var authUser = authResult.Value.AuthenticatedUser;
        var handler = httpContext.RequestServices.GetRequiredService<GetHouseholdMembersHandler>();
        var result = await handler.HandleAsync(
            new GetHouseholdMembersQuery(new HouseholdId(householdId), authUser.UserId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                HouseholdErrors.HouseholdNotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status403Forbidden
            };

            await WriteProblemAsync(
                httpContext,
                statusCode,
                "Request rejected.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode!,
                HouseholdErrors.ValidationError);
            return;
        }

        var response = new GetHouseholdMembersResponse(
            result.Members!.Select(member => new HouseholdMemberResponse(member.UserId, member.Role)).ToArray());
        await TypedResults.Ok(response).ExecuteAsync(httpContext);
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
                HouseholdErrors.ValidationError,
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
                    HouseholdErrors.ValidationError,
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
                HouseholdErrors.ValidationError,
                httpContext.Request.Path));
        }
        catch (NotSupportedException)
        {
            return (false, default, BuildProblemResult(
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "The request payload is invalid.",
                HouseholdErrors.ValidationError,
                httpContext.Request.Path));
        }
        catch (BadHttpRequestException)
        {
            return (false, default, BuildProblemResult(
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "The request payload is invalid.",
                HouseholdErrors.ValidationError,
                httpContext.Request.Path));
        }
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

    private static bool IsJsonContentType(string contentType)
    {
        var mediaType = contentType.Split(';')[0].Trim();
        return mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase);
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

public sealed record CreateHouseholdRequest(string Name);

public sealed record CreateHouseholdResponse(Guid Id, string Name, DateTimeOffset CreatedAtUtc);

public sealed record InviteHouseholdMemberRequest(string InviteeEmail);

public sealed record InviteHouseholdMemberResponse(string InvitationToken, DateTimeOffset ExpiresAtUtc);

public sealed record HouseholdMemberResponse(Guid UserId, string Role);

public sealed record GetHouseholdMembersResponse(HouseholdMemberResponse[] Members);

internal sealed record AuthenticatedUser(Guid UserId, string Email);
