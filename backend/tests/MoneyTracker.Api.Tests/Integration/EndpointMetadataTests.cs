using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace MoneyTracker.Api.Tests.Integration;

public sealed class EndpointMetadataTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public void HouseholdsEndpoint_ContainsOpenApiReadyContractMetadata()
    {
        using var factory = new MoneyTrackerApiFactory();
        var endpointDataSource = factory.Services.GetRequiredService<EndpointDataSource>();

        var householdsEndpoint = endpointDataSource.Endpoints.Single(endpoint =>
        {
            var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();
            return methods is not null
                && methods.HttpMethods.Contains("POST", StringComparer.Ordinal)
                && string.Equals(endpoint.DisplayName, "HTTP: POST /households", StringComparison.Ordinal);
        });

        var acceptsMetadata = householdsEndpoint.Metadata.GetOrderedMetadata<IAcceptsMetadata>();
        Assert.Contains(acceptsMetadata, metadata => metadata.ContentTypes.Contains("application/json", StringComparer.Ordinal));

        var producesMetadata = householdsEndpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();
        Assert.Contains(producesMetadata, metadata => metadata.StatusCode == StatusCodes.Status201Created);
        Assert.Contains(producesMetadata, metadata => metadata.StatusCode == StatusCodes.Status400BadRequest);
        Assert.Contains(producesMetadata, metadata => metadata.StatusCode == StatusCodes.Status409Conflict);
    }
}
