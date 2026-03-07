using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Modules.Experiments.Application.AllocateUser;
using MoneyTracker.Modules.Experiments.Application.CreateExperiment;
using MoneyTracker.Modules.Experiments.Application.GetActiveAllocations;
using MoneyTracker.Modules.Experiments.Application.GetExperimentResults;
using MoneyTracker.Modules.Experiments.Application.RecordConversion;
using MoneyTracker.Modules.Experiments.Application.UpdateExperiment;
using MoneyTracker.Modules.Experiments.Domain;
using MoneyTracker.Modules.SharedKernel.Presentation;

namespace MoneyTracker.Modules.Experiments.Presentation;

public static class ExperimentEndpoints
{
    public static IServiceCollection AddExperimentsModule(this IServiceCollection services)
    {
        services.AddSingleton<IExperimentRepository, Infrastructure.InMemoryExperimentRepository>();
        services.AddScoped<AllocateUserHandler>();
        services.AddScoped<GetActiveAllocationsHandler>();
        services.AddScoped<RecordConversionHandler>();
        services.AddScoped<GetExperimentResultsHandler>();
        services.AddScoped<CreateExperimentHandler>();
        services.AddScoped<UpdateExperimentHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapExperimentEndpoints(this IEndpointRouteBuilder app)
    {
        var allocateEndpoint = (RouteHandlerBuilder)app.MapPost("/experiments/allocate", AllocateUser);
        allocateEndpoint
            .WithName("AllocateUserToExperiment")
            .WithSummary("Allocate user to experiment variant.")
            .WithDescription("Deterministically assigns a user to an experiment variant using SHA256 hashing. Returns existing allocation if already assigned.")
            .Accepts<AllocateUserRequest>("application/json")
            .Produces<AllocateUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        var activeAllocationsEndpoint = (RouteHandlerBuilder)app.MapGet("/experiments/active", GetActiveAllocations);
        activeAllocationsEndpoint
            .WithName("GetActiveAllocations")
            .WithSummary("Get all active experiment allocations.")
            .WithDescription("Returns all allocations for active experiments for the authenticated user.")
            .Produces<ActiveAllocationsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        var convertEndpoint = (RouteHandlerBuilder)app.MapPost("/experiments/convert", RecordConversion);
        convertEndpoint
            .WithName("RecordExperimentConversion")
            .WithSummary("Record experiment conversion.")
            .WithDescription("Records a conversion event for the authenticated user. Idempotent — duplicate conversions are silently ignored.")
            .Accepts<RecordConversionRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        var createExperimentEndpoint = (RouteHandlerBuilder)app.MapPost("/admin/experiments", CreateExperiment);
        createExperimentEndpoint
            .WithName("CreateExperiment")
            .WithSummary("Create a new experiment.")
            .WithDescription("Creates a new A/B test experiment. Admin access required.")
            .Accepts<CreateExperimentRequest>("application/json")
            .Produces<CreateExperimentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        var updateExperimentEndpoint = (RouteHandlerBuilder)app.MapMethods("/admin/experiments/{id}", ["PATCH"], UpdateExperiment);
        updateExperimentEndpoint
            .WithName("UpdateExperimentStatus")
            .WithSummary("Update experiment status.")
            .WithDescription("Updates the status of an experiment (e.g., Draft to Active). Admin access required.")
            .Accepts<UpdateExperimentRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        var resultsEndpoint = (RouteHandlerBuilder)app.MapGet("/admin/experiments/{id}/results", GetExperimentResults);
        resultsEndpoint
            .WithName("GetExperimentResults")
            .WithSummary("Get experiment results with significance.")
            .WithDescription("Returns per-variant conversion rates and chi-squared significance test results. Admin access required.")
            .Produces<ExperimentResultsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task AllocateUser(HttpContext httpContext)
    {
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var (isValidRequest, request, parseProblem) =
            await EndpointHelpers.ReadJsonRequestAsync<AllocateUserRequest>(httpContext, ExperimentErrors.ValidationError);
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
                    ExperimentErrors.ValidationError,
                    ExperimentErrors.ValidationError);
            }
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<AllocateUserHandler>();
        var result = await handler.HandleAsync(
            new AllocateUserCommand(
                new ExperimentId(request.ExperimentId),
                authResult.AuthenticatedUser!.UserId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                ExperimentErrors.ExperimentNotFound => StatusCodes.Status404NotFound,
                ExperimentErrors.ExperimentNotActive => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status400BadRequest
            };

            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
                "Allocation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                ExperimentErrors.ValidationError);
            return;
        }

        var response = new AllocateUserResponse(
            result.ExperimentId!.Value.Value,
            result.ExperimentName!,
            result.VariantName!,
            result.AllocatedAtUtc!.Value);
        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static async Task GetActiveAllocations(HttpContext httpContext)
    {
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<GetActiveAllocationsHandler>();
        var result = await handler.HandleAsync(
            new GetActiveAllocationsQuery(authResult.AuthenticatedUser!.UserId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Request failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                ExperimentErrors.ValidationError);
            return;
        }

        var response = new ActiveAllocationsResponse(
            result.Allocations!
                .Select(a => new AllocationResponse(
                    a.ExperimentId,
                    a.ExperimentName,
                    a.VariantName,
                    a.AllocatedAtUtc))
                .ToArray());
        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static async Task RecordConversion(HttpContext httpContext)
    {
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var (isValidRequest, request, parseProblem) =
            await EndpointHelpers.ReadJsonRequestAsync<RecordConversionRequest>(httpContext, ExperimentErrors.ValidationError);
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
                    ExperimentErrors.ValidationError,
                    ExperimentErrors.ValidationError);
            }
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<RecordConversionHandler>();
        var result = await handler.HandleAsync(
            new RecordConversionCommand(
                new ExperimentId(request.ExperimentId),
                authResult.AuthenticatedUser!.UserId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                ExperimentErrors.ExperimentNotFound => StatusCodes.Status404NotFound,
                ExperimentErrors.AllocationNotFound => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status400BadRequest
            };

            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
                "Conversion failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                ExperimentErrors.ValidationError);
            return;
        }

        httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
    }

    private static async Task CreateExperiment(HttpContext httpContext)
    {
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var adminService = httpContext.RequestServices.GetRequiredService<IAdminAccessService>();
        var isAdmin = await adminService.IsAdminAsync(authResult.AuthenticatedUser!.UserId, httpContext.RequestAborted);
        if (!isAdmin)
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status403Forbidden,
                "Access denied.",
                "Admin access is required to create experiments.",
                ExperimentErrors.AccessDenied,
                ExperimentErrors.AccessDenied);
            return;
        }

        var (isValidRequest, request, parseProblem) =
            await EndpointHelpers.ReadJsonRequestAsync<CreateExperimentRequest>(httpContext, ExperimentErrors.ValidationError);
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
                    ExperimentErrors.ValidationError,
                    ExperimentErrors.ValidationError);
            }
            return;
        }

        var variants = request.Variants
            .Select(v => new ExperimentVariant(v.Name, v.Weight))
            .ToList();

        var handler = httpContext.RequestServices.GetRequiredService<CreateExperimentHandler>();
        var result = await handler.HandleAsync(
            new CreateExperimentCommand(
                request.Name,
                request.Description,
                variants,
                request.TargetMetric,
                request.StartDate,
                request.EndDate),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                ExperimentErrors.ValidationError);
            return;
        }

        var response = new CreateExperimentResponse(result.ExperimentId!.Value);
        httpContext.Response.StatusCode = StatusCodes.Status201Created;
        await httpContext.Response.WriteAsJsonAsync(response, httpContext.RequestAborted);
    }

    private static async Task UpdateExperiment(HttpContext httpContext)
    {
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var adminService = httpContext.RequestServices.GetRequiredService<IAdminAccessService>();
        var isAdmin = await adminService.IsAdminAsync(authResult.AuthenticatedUser!.UserId, httpContext.RequestAborted);
        if (!isAdmin)
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status403Forbidden,
                "Access denied.",
                "Admin access is required to update experiments.",
                ExperimentErrors.AccessDenied,
                ExperimentErrors.AccessDenied);
            return;
        }

        if (!TryGetGuidRoute(httpContext, "id", out var experimentId))
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "Experiment ID route parameter is required.",
                ExperimentErrors.ValidationError,
                ExperimentErrors.ValidationError);
            return;
        }

        var (isValidRequest, request, parseProblem) =
            await EndpointHelpers.ReadJsonRequestAsync<UpdateExperimentRequest>(httpContext, ExperimentErrors.ValidationError);
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
                    ExperimentErrors.ValidationError,
                    ExperimentErrors.ValidationError);
            }
            return;
        }

        if (!Enum.TryParse<ExperimentStatus>(request.Status, ignoreCase: true, out var status))
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                $"Invalid status value: {request.Status}.",
                ExperimentErrors.ValidationError,
                ExperimentErrors.ValidationError);
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<UpdateExperimentHandler>();
        var result = await handler.HandleAsync(
            new UpdateExperimentCommand(
                new ExperimentId(experimentId),
                status),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                ExperimentErrors.ExperimentNotFound => StatusCodes.Status404NotFound,
                ExperimentErrors.ExperimentInvalidStateTransition => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status400BadRequest
            };

            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
                "Update failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                ExperimentErrors.ValidationError);
            return;
        }

        httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
    }

    private static async Task GetExperimentResults(HttpContext httpContext)
    {
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var adminService = httpContext.RequestServices.GetRequiredService<IAdminAccessService>();
        var isAdmin = await adminService.IsAdminAsync(authResult.AuthenticatedUser!.UserId, httpContext.RequestAborted);
        if (!isAdmin)
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status403Forbidden,
                "Access denied.",
                "Admin access is required to view experiment results.",
                ExperimentErrors.AccessDenied,
                ExperimentErrors.AccessDenied);
            return;
        }

        if (!TryGetGuidRoute(httpContext, "id", out var experimentId))
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "Experiment ID route parameter is required.",
                ExperimentErrors.ValidationError,
                ExperimentErrors.ValidationError);
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<GetExperimentResultsHandler>();
        var result = await handler.HandleAsync(
            new GetExperimentResultsQuery(new ExperimentId(experimentId)),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                ExperimentErrors.ExperimentNotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status400BadRequest
            };

            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
                "Request failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                ExperimentErrors.ValidationError);
            return;
        }

        var response = new ExperimentResultsResponse(
            result.ExperimentId!.Value,
            result.ExperimentName!,
            result.Variants!
                .Select(v => new VariantResultResponse(
                    v.VariantName,
                    v.TotalAllocations,
                    v.Conversions,
                    v.ConversionRate))
                .ToArray(),
            result.ChiSquaredStatistic!.Value,
            result.PValue!.Value,
            result.IsSignificant!.Value,
            result.SampleSizeWarning!.Value);
        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static bool TryGetGuidRoute(HttpContext httpContext, string key, out Guid value)
    {
        value = Guid.Empty;
        var raw = httpContext.Request.RouteValues[key]?.ToString();
        return raw is not null && Guid.TryParse(raw, out value);
    }
}

// Request DTOs
public sealed record AllocateUserRequest(Guid ExperimentId);

public sealed record RecordConversionRequest(Guid ExperimentId);

public sealed record CreateExperimentRequest(
    string Name,
    string Description,
    VariantRequest[] Variants,
    string TargetMetric,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate);

public sealed record VariantRequest(string Name, int Weight);

public sealed record UpdateExperimentRequest(string Status);

// Response DTOs
public sealed record AllocateUserResponse(
    Guid ExperimentId,
    string ExperimentName,
    string VariantName,
    DateTimeOffset AllocatedAtUtc);

public sealed record ActiveAllocationsResponse(AllocationResponse[] Allocations);

public sealed record AllocationResponse(
    Guid ExperimentId,
    string ExperimentName,
    string VariantName,
    DateTimeOffset AllocatedAtUtc);

public sealed record CreateExperimentResponse(Guid ExperimentId);

public sealed record ExperimentResultsResponse(
    Guid ExperimentId,
    string ExperimentName,
    VariantResultResponse[] Variants,
    double ChiSquaredStatistic,
    double PValue,
    bool IsSignificant,
    bool SampleSizeWarning);

public sealed record VariantResultResponse(
    string VariantName,
    int TotalAllocations,
    int Conversions,
    double ConversionRate);
