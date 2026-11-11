using Kongroo.Catalog.Application;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Kongroo.Catalog.Presentation;

public sealed class GameSearchUnavailableExceptionHandler(
    ILogger<GameSearchUnavailableExceptionHandler> logger,
    IProblemDetailsService problemDetailsService
) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        if (exception is not GameSearchUnavailableException)
        {
            return false;
        }

        logger.LogWarning(exception, "Game Search is unavailable");
        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

        return await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails { Detail = "Game Search is temporarily unavailable." },
            }
        );
    }
}
