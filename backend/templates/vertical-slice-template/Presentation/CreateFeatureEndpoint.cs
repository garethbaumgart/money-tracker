using MoneyTracker.Modules.Feature.Application.CreateFeature;

namespace MoneyTracker.Modules.Feature.Presentation;

public static class CreateFeatureEndpoint
{
    public static IEndpointRouteBuilder MapFeatureEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/features",
            async (CreateFeatureRequest request, CreateFeatureHandler handler, CancellationToken ct) =>
            {
                var id = await handler.HandleAsync(new CreateFeatureCommand(request.Name), ct);
                return Results.Created($"/features/{id}", new { id });
            });

        return app;
    }
}

public sealed record CreateFeatureRequest(string Name);
