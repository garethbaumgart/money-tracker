using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Modules.Households.Application.CreateHousehold;
using MoneyTracker.Modules.Households.Domain;
using System.Text.Json;

namespace MoneyTracker.Modules.Households.Presentation;

public static class CreateHouseholdEndpoint
{
    public static IServiceCollection AddHouseholdsModule(this IServiceCollection services)
    {
        services.AddSingleton<IHouseholdRepository, Infrastructure.InMemoryHouseholdRepository>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CreateHouseholdHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapHouseholdEndpoints(this IEndpointRouteBuilder app)
    {
        Delegate createHouseholdHandler = async (HttpContext httpContext) =>
        {
            CreateHouseholdRequest? request;
            try
            {
                request = await httpContext.Request.ReadFromJsonAsync<CreateHouseholdRequest>(cancellationToken: httpContext.RequestAborted);
            }
            catch (JsonException)
            {
                var malformedPayloadResult = TypedResults.Problem(CreateProblemDetails(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Validation failed.",
                    detail: "The request payload is invalid.",
                    code: HouseholdErrors.ValidationError,
                    instance: httpContext.Request.Path));

                await malformedPayloadResult.ExecuteAsync(httpContext);
                return;
            }

            var handler = httpContext.RequestServices.GetRequiredService<CreateHouseholdHandler>();
            IResult httpResult;

            if (request is null)
            {
                httpResult = TypedResults.Problem(CreateProblemDetails(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Validation failed.",
                    detail: "The request payload is invalid.",
                    code: HouseholdErrors.ValidationError,
                    instance: httpContext.Request.Path));

                await httpResult.ExecuteAsync(httpContext);
                return;
            }

            var createResult = await handler.HandleAsync(new CreateHouseholdCommand(request.Name), httpContext.RequestAborted);
            if (createResult.IsSuccess)
            {
                var household = createResult.Household!;
                httpResult = TypedResults.Created(
                    $"/households/{household.Id.Value}",
                    new CreateHouseholdResponse(
                        household.Id.Value,
                        household.Name,
                        household.CreatedAtUtc));

                await httpResult.ExecuteAsync(httpContext);
                return;
            }

            if (createResult.ErrorCode == HouseholdErrors.HouseholdNameConflict)
            {
                httpResult = TypedResults.Problem(CreateProblemDetails(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Household already exists.",
                    detail: createResult.ErrorMessage!,
                    code: HouseholdErrors.HouseholdNameConflict,
                    instance: httpContext.Request.Path));

                await httpResult.ExecuteAsync(httpContext);
                return;
            }

            httpResult = TypedResults.Problem(CreateProblemDetails(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation failed.",
                detail: createResult.ErrorMessage ?? "The request payload is invalid.",
                code: HouseholdErrors.ValidationError,
                instance: httpContext.Request.Path));

            await httpResult.ExecuteAsync(httpContext);
        };

        app.MapPost(
                "/households",
                createHouseholdHandler)
            .WithName("CreateHousehold")
            .WithSummary("Create a household.")
            .WithDescription("Creates a household with a unique case-insensitive name.")
            .Accepts<CreateHouseholdRequest>("application/json")
            .Produces<CreateHouseholdResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static ProblemDetails CreateProblemDetails(int statusCode, string title, string detail, string code, string instance)
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
}

public sealed record CreateHouseholdRequest(string Name);

public sealed record CreateHouseholdResponse(Guid Id, string Name, DateTimeOffset CreatedAtUtc);
