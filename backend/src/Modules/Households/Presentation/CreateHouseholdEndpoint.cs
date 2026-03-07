using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Modules.Households.Application.AcceptHouseholdInvitation;
using MoneyTracker.Modules.Households.Application.CreateHousehold;
using MoneyTracker.Modules.Households.Application.GetCurrentBudgetSnapshot;
using MoneyTracker.Modules.Households.Application.GetHouseholdDashboard;
using MoneyTracker.Modules.Households.Application.GetHouseholdMembers;
using MoneyTracker.Modules.Households.Application.InviteHouseholdMember;
using MoneyTracker.Modules.Households.Domain;
using MoneyTracker.Modules.SharedKernel.Households;
using MoneyTracker.Modules.SharedKernel.Presentation;

namespace MoneyTracker.Modules.Households.Presentation;

public static class CreateHouseholdEndpoint
{
    public static IServiceCollection AddHouseholdsModule(this IServiceCollection services)
    {
        services.AddSingleton<IHouseholdRepository, Infrastructure.InMemoryHouseholdRepository>();
        services.AddScoped<IHouseholdAccessService, Infrastructure.HouseholdAccessService>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CreateHouseholdHandler>();
        services.AddScoped<InviteHouseholdMemberHandler>();
        services.AddScoped<AcceptHouseholdInvitationHandler>();
        services.AddScoped<GetHouseholdMembersHandler>();
        services.AddScoped<GetCurrentBudgetSnapshotHandler>();
        services.AddScoped<GetHouseholdDashboardHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapHouseholdEndpoints(this IEndpointRouteBuilder app)
    {
        var createHouseholdEndpoint = (RouteHandlerBuilder)app.MapPost("/households", CreateHousehold);
        createHouseholdEndpoint
            .WithName("CreateHousehold")
            .WithSummary("Create a household.")
            .WithDescription("Creates a household owned by the authenticated user.")
            .Accepts<CreateHouseholdRequest>("application/json")
            .Produces<CreateHouseholdResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        app.MapPost("/households/{householdId:guid}/invite", InviteHouseholdMember)
            .WithName("InviteHouseholdMember")
            .WithSummary("Invite a user to a household.")
            .WithDescription("Only the household owner may issue an invitation token.");

        app.MapPost("/households/invitations/{invitationToken}/accept", AcceptHouseholdInvitation)
            .WithName("AcceptHouseholdInvitation")
            .WithSummary("Accept a household invitation.")
            .WithDescription("Adds the authenticated user as a household member.");

        app.MapGet("/households/{householdId:guid}/members", GetHouseholdMembers)
            .WithName("GetHouseholdMembers")
            .WithSummary("Get household members.")
            .WithDescription("Returns all members for an existing household.");

        app.MapHouseholdDashboardEndpoints();

        return app;
    }

    private static async Task CreateHousehold(HttpContext httpContext)
    {
        var authResult = await HouseholdEndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var authUser = authResult.AuthenticatedUser;
        if (authUser is null)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var (isValidRequest, request, parseProblem) =
            await EndpointHelpers.ReadJsonRequestAsync<CreateHouseholdRequest>(httpContext, HouseholdErrors.ValidationError);
        if (!isValidRequest || request is null)
        {
            if (parseProblem is not null)
            {
                await parseProblem.ExecuteAsync(httpContext);
            }
            else
            {
                await HouseholdEndpointHelpers.WriteProblemAsync(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Validation failed.",
                    "The request payload is required.",
                    HouseholdErrors.ValidationError,
                    HouseholdErrors.ValidationError);
            }
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<CreateHouseholdHandler>();
        var householdRequest = request;
        var result = await handler.HandleAsync(
            new CreateHouseholdCommand(householdRequest.Name, authUser.UserId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                HouseholdErrors.HouseholdNameConflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };

            await HouseholdEndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
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
        var authResult = await HouseholdEndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var authUser = authResult.AuthenticatedUser;
        if (authUser is null)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var (isValidRequest, request, parseProblem) =
            await EndpointHelpers.ReadJsonRequestAsync<InviteHouseholdMemberRequest>(httpContext, HouseholdErrors.ValidationError);
        if (!isValidRequest || request is null)
        {
            if (parseProblem is not null)
            {
                await parseProblem.ExecuteAsync(httpContext);
            }
            else
            {
                await HouseholdEndpointHelpers.WriteProblemAsync(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Validation failed.",
                    "The request payload is required.",
                    HouseholdErrors.ValidationError,
                    HouseholdErrors.ValidationError);
            }
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<InviteHouseholdMemberHandler>();
        var result = await handler.HandleAsync(
            new InviteHouseholdMemberCommand(
                new HouseholdId(householdId),
                authUser.UserId,
                request.InviteeEmail),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                HouseholdErrors.HouseholdNotFound => StatusCodes.Status404NotFound,
                HouseholdErrors.HouseholdAccessDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };

            await HouseholdEndpointHelpers.WriteProblemAsync(
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
        var authResult = await HouseholdEndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var authUser = authResult.AuthenticatedUser;
        if (authUser is null)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

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

            await HouseholdEndpointHelpers.WriteProblemAsync(
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
        var authResult = await HouseholdEndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var authUser = authResult.AuthenticatedUser;
        if (authUser is null)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

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

            await HouseholdEndpointHelpers.WriteProblemAsync(
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
}

public sealed record CreateHouseholdRequest(string Name);

public sealed record CreateHouseholdResponse(Guid Id, string Name, DateTimeOffset CreatedAtUtc);

public sealed record InviteHouseholdMemberRequest(string InviteeEmail);

public sealed record InviteHouseholdMemberResponse(string InvitationToken, DateTimeOffset ExpiresAtUtc);

public sealed record HouseholdMemberResponse(Guid UserId, string Role);

public sealed record GetHouseholdMembersResponse(HouseholdMemberResponse[] Members);
