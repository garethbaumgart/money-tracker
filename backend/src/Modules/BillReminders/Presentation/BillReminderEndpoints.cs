using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Modules.Auth.Application.GetAuthenticatedUser;
using MoneyTracker.Modules.Auth.Domain;
using MoneyTracker.Modules.BillReminders.Application.CreateBillReminder;
using MoneyTracker.Modules.BillReminders.Application.GetBillReminders;
using MoneyTracker.Modules.BillReminders.Application.DispatchDueReminders;
using MoneyTracker.Modules.BillReminders.Domain;

namespace MoneyTracker.Modules.BillReminders.Presentation;

public static class BillReminderEndpoints
{
    public static IServiceCollection AddBillRemindersModule(this IServiceCollection services)
    {
        services.AddSingleton<IBillReminderRepository, Infrastructure.InMemoryBillReminderRepository>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CreateBillReminderHandler>();
        services.AddScoped<GetBillRemindersHandler>();
        services.AddSingleton<DispatchDueRemindersHandler>();
        services.AddHostedService<Infrastructure.BillReminderDispatchWorker>();

        return services;
    }

    public static IEndpointRouteBuilder MapBillReminderEndpoints(this IEndpointRouteBuilder app)
    {
        var createReminderEndpoint = (RouteHandlerBuilder)app.MapPost(
            "/households/{householdId:guid}/bill-reminders",
            CreateBillReminder);
        createReminderEndpoint
            .WithName("CreateBillReminder")
            .WithSummary("Create a bill reminder.")
            .WithDescription("Creates a household-scoped bill reminder.")
            .Accepts<CreateBillReminderRequest>("application/json")
            .Produces<BillReminderResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapGet(
                "/households/{householdId:guid}/bill-reminders",
                GetBillReminders)
            .WithName("GetBillReminders")
            .WithSummary("List bill reminders.")
            .WithDescription("Returns household-scoped bill reminders.");

        return app;
    }

    private static async Task CreateBillReminder(HttpContext httpContext, Guid householdId)
    {
        var authResult = await ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var (isValidRequest, request, parseProblem) =
            await ReadJsonRequestAsync<CreateBillReminderRequest>(httpContext);
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
                    BillReminderErrors.ValidationError,
                    BillReminderErrors.ValidationError);
            }
            return;
        }

        if (!TryParseCadence(request.Cadence, out var cadence))
        {
            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "Reminder cadence is invalid.",
                BillReminderErrors.ReminderCadenceInvalid,
                BillReminderErrors.ValidationError);
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<CreateBillReminderHandler>();
        var result = await handler.HandleAsync(
            new CreateBillReminderCommand(
                householdId,
                request.Title,
                request.Amount,
                request.DueDateUtc,
                cadence,
                authResult.AuthenticatedUser!.UserId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                BillReminderErrors.ReminderHouseholdNotFound => StatusCodes.Status404NotFound,
                BillReminderErrors.ReminderAccessDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };

            await WriteProblemAsync(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status403Forbidden ? "Access denied." : "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                BillReminderErrors.ValidationError);
            return;
        }

        var reminder = result.Reminder!;
        var timeProvider = httpContext.RequestServices.GetRequiredService<TimeProvider>();
        var response = BuildReminderResponse(reminder, timeProvider.GetUtcNow());
        await TypedResults.Created(
                $"/households/{householdId}/bill-reminders/{reminder.Id.Value}",
                response)
            .ExecuteAsync(httpContext);
    }

    private static async Task GetBillReminders(HttpContext httpContext, Guid householdId)
    {
        var authResult = await ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<GetBillRemindersHandler>();
        var result = await handler.HandleAsync(
            new GetBillRemindersQuery(householdId, authResult.AuthenticatedUser!.UserId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                BillReminderErrors.ReminderHouseholdNotFound => StatusCodes.Status404NotFound,
                BillReminderErrors.ReminderAccessDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };

            await WriteProblemAsync(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status403Forbidden ? "Access denied." : "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                BillReminderErrors.ValidationError);
            return;
        }

        var timeProvider = httpContext.RequestServices.GetRequiredService<TimeProvider>();
        var nowUtc = timeProvider.GetUtcNow();
        var response = new BillRemindersResponse(
            result.Reminders!
                .Select(reminder => BuildReminderResponse(reminder, nowUtc))
                .ToArray());
        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static BillReminderResponse BuildReminderResponse(BillReminder reminder, DateTimeOffset nowUtc)
    {
        return new BillReminderResponse(
            reminder.Id.Value,
            reminder.Title,
            reminder.Amount,
            reminder.Cadence.ToString(),
            reminder.NextDueDateUtc,
            reminder.LastNotifiedAtUtc,
            reminder.IsOverdue(nowUtc),
            reminder.DispatchAttemptCount,
            reminder.LastDispatchErrorCode,
            reminder.LastDispatchErrorMessage,
            reminder.CreatedAtUtc);
    }

    private static bool TryParseCadence(string cadence, out BillReminderCadence parsed)
    {
        parsed = BillReminderCadence.Once;
        if (string.IsNullOrWhiteSpace(cadence))
        {
            return false;
        }

        return Enum.TryParse(cadence, ignoreCase: true, out parsed);
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
                BillReminderErrors.ValidationError,
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
                    BillReminderErrors.ValidationError,
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
                BillReminderErrors.ValidationError,
                httpContext.Request.Path));
        }
        catch (NotSupportedException)
        {
            return (false, default, BuildProblemResult(
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "The request payload is invalid.",
                BillReminderErrors.ValidationError,
                httpContext.Request.Path));
        }
        catch (BadHttpRequestException)
        {
            return (false, default, BuildProblemResult(
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "The request payload is invalid.",
                BillReminderErrors.ValidationError,
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

public sealed record CreateBillReminderRequest(
    string Title,
    decimal Amount,
    DateTimeOffset DueDateUtc,
    string Cadence);

public sealed record BillReminderResponse(
    Guid Id,
    string Title,
    decimal Amount,
    string Cadence,
    DateTimeOffset NextDueDateUtc,
    DateTimeOffset? LastNotifiedAtUtc,
    bool IsOverdue,
    int DispatchAttemptCount,
    string? LastDispatchErrorCode,
    string? LastDispatchErrorMessage,
    DateTimeOffset CreatedAtUtc);

public sealed record BillRemindersResponse(BillReminderResponse[] Reminders);

internal sealed record AuthenticatedUser(Guid UserId, string Email);
