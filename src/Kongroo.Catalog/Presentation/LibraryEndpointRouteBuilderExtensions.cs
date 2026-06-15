using System.ComponentModel;
using System.Security.Claims;
using Kongroo.BuildingBlocks.Presentation.Authorization;
using Kongroo.Catalog.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Kongroo.Catalog.Presentation;

public static class LibraryEndpointRouteBuilderExtensions
{
    extension(IEndpointRouteBuilder endpoints)
    {
        public RouteGroupBuilder MapLibraryEndpoints()
        {
            var routeGroup = endpoints.MapGroup("/library").WithTags("Library");
            var ownershipsGroup = routeGroup.MapGroup("/ownerships");

            ownershipsGroup
                .MapGet("/", GetOwnershipsAsync)
                .RequireAuthorization()
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithName("GetOwnerships")
                .WithSummary("Get ownerships")
                .WithDescription(
                    "Returns the authenticated user's library ownership records ordered by most recent acquisition."
                );

            ownershipsGroup
                .MapGet("/{ownershipId:guid}", GetOwnershipAsync)
                .RequireAuthorization()
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithName("GetOwnershipById")
                .WithSummary("Get an ownership")
                .WithDescription("Returns a single ownership record owned by the authenticated user.");

            return routeGroup;
        }
    }

    private static async Task<Ok<IReadOnlyList<GetOwnershipResponse>>> GetOwnershipsAsync(
        ClaimsPrincipal user,
        GetOwnershipsQueryHandler handler,
        CancellationToken cancellationToken
    )
    {
        var query = new GetOwnershipsQuery(user.GetUserId());
        var response = await handler.HandleAsync(query, cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<GetOwnershipResponse>> GetOwnershipAsync(
        [Description("Unique identifier of the ownership to retrieve.")] Guid ownershipId,
        ClaimsPrincipal user,
        GetOwnershipQueryHandler handler,
        CancellationToken cancellationToken
    )
    {
        var query = new GetOwnershipQuery(user.GetUserId(), ownershipId);
        var response = await handler.HandleAsync(query, cancellationToken);

        return TypedResults.Ok(response);
    }
}
