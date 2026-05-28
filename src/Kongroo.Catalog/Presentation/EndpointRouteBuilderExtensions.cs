using System.ComponentModel;
using System.Security.Claims;
using Kongroo.BuildingBlocks.Presentation.Authorization;
using Kongroo.Catalog.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Kongroo.Catalog.Presentation;

public static class EndpointRouteBuilderExtensions
{
    extension(IEndpointRouteBuilder endpoints)
    {
        public RouteGroupBuilder MapCatalogEndpoints()
        {
            var routeGroup = endpoints.MapGroup("/catalog").WithTags("Catalog");

            var gamesGroup = routeGroup.MapGroup("/games");
            var ordersGroup = routeGroup.MapGroup("/orders");

            gamesGroup
                .MapPost("/", CreateGameAsync)
                .RequireAuthorization(AuthorizationPolicies.AdminOnly)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithName("CreateGame")
                .WithSummary("Create a game")
                .WithDescription("Creates a new game and returns its public catalog representation.");

            gamesGroup
                .MapGet("/", GetGamesAsync)
                .RequireAuthorization()
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithName("GetGames")
                .WithSummary("Get games")
                .WithDescription("Returns all games ordered by title.");

            gamesGroup
                .MapGet("/{gameId:guid}", GetGameAsync)
                .RequireAuthorization()
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithName("GetGameById")
                .WithSummary("Get a game")
                .WithDescription("Returns a single game from the catalog.");

            gamesGroup
                .MapPut("/{gameId:guid}", UpdateGameAsync)
                .RequireAuthorization(AuthorizationPolicies.AdminOnly)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithName("UpdateGame")
                .WithSummary("Update a game")
                .WithDescription("Replaces the editable details of an existing game.");

            gamesGroup
                .MapPost("/{gameId:guid}/promotions", CreatePromotionAsync)
                .RequireAuthorization(AuthorizationPolicies.AdminOnly)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithName("CreatePromotion")
                .WithSummary("Create a promotion")
                .WithDescription("Creates a scheduled promotion for an existing game.");

            gamesGroup
                .MapDelete("/{gameId:guid}", DeleteGameAsync)
                .RequireAuthorization(AuthorizationPolicies.AdminOnly)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithName("DeleteGame")
                .WithSummary("Delete a game")
                .WithDescription("Deletes an existing game from the catalog.");

            ordersGroup
                .MapGet("/", GetOrdersAsync)
                .RequireAuthorization()
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithName("GetOrders")
                .WithSummary("Get orders")
                .WithDescription("Returns the authenticated user's orders ordered by most recent purchase.");

            ordersGroup
                .MapGet("/{orderId:guid}", GetOrderAsync)
                .RequireAuthorization()
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithName("GetOrderById")
                .WithSummary("Get an order")
                .WithDescription("Returns a single order owned by the authenticated user.");

            ordersGroup
                .MapPost("/", PlaceOrderAsync)
                .RequireAuthorization()
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithName("PlaceOrder")
                .WithSummary("Place an order")
                .WithDescription("Purchases one or more games for the authenticated user.");

            return routeGroup;
        }
    }

    private static async Task<CreatedAtRoute<CreateGameResponse>> CreateGameAsync(
        CreateGameRequest request,
        CreateGameCommandHandler handler,
        CancellationToken cancellationToken
    )
    {
        var command = new CreateGameCommand(request.Title, request.Description, request.PriceAmount, request.Currency);
        var response = await handler.HandleAsync(command, cancellationToken);

        return TypedResults.CreatedAtRoute(response, "GetGameById", new { gameId = response.Id });
    }

    private static async Task<Ok<IReadOnlyList<GetGameResponse>>> GetGamesAsync(
        GetGamesQueryHandler handler,
        CancellationToken cancellationToken
    )
    {
        var query = new GetGamesQuery();
        var response = await handler.HandleAsync(query, cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<GetGameResponse>> GetGameAsync(
        [Description("Unique identifier of the game to retrieve.")] Guid gameId,
        GetGameQueryHandler handler,
        CancellationToken cancellationToken
    )
    {
        var query = new GetGameQuery(gameId);
        var response = await handler.HandleAsync(query, cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<GetGameResponse>> UpdateGameAsync(
        [Description("Unique identifier of the game to update.")] Guid gameId,
        UpdateGameRequest request,
        UpdateGameCommandHandler handler,
        CancellationToken cancellationToken
    )
    {
        var command = new UpdateGameCommand(
            gameId,
            request.Title,
            request.Description,
            request.PriceAmount,
            request.Currency,
            request.Status
        );
        var response = await handler.HandleAsync(command, cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<GetPromotionResponse>> CreatePromotionAsync(
        [Description("Unique identifier of the game to promote.")] Guid gameId,
        CreatePromotionRequest request,
        CreatePromotionCommandHandler handler,
        CancellationToken cancellationToken
    )
    {
        var command = new CreatePromotionCommand(gameId, request.Discount, request.StartsAt, request.EndsAt);
        var response = await handler.HandleAsync(command, cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<NoContent> DeleteGameAsync(
        [Description("Unique identifier of the game to delete.")] Guid gameId,
        DeleteGameCommandHandler handler,
        CancellationToken cancellationToken
    )
    {
        var command = new DeleteGameCommand(gameId);
        await handler.HandleAsync(command, cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Ok<IReadOnlyList<GetOrderResponse>>> GetOrdersAsync(
        ClaimsPrincipal user,
        GetOrdersQueryHandler handler,
        CancellationToken cancellationToken
    )
    {
        var query = new GetOrdersQuery(user.GetUserId());
        var response = await handler.HandleAsync(query, cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<GetOrderResponse>> GetOrderAsync(
        [Description("Unique identifier of the order to retrieve.")] Guid orderId,
        ClaimsPrincipal user,
        GetOrderQueryHandler handler,
        CancellationToken cancellationToken
    )
    {
        var query = new GetOrderQuery(user.GetUserId(), orderId);
        var response = await handler.HandleAsync(query, cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<GetOrderResponse>> PlaceOrderAsync(
        PlaceOrderRequest request,
        ClaimsPrincipal user,
        PlaceOrderCommandHandler handler,
        CancellationToken cancellationToken
    )
    {
        var command = new PlaceOrderCommand(user.GetUserId(), request.GameIds);
        var response = await handler.HandleAsync(command, cancellationToken);

        return TypedResults.Ok(response);
    }
}

