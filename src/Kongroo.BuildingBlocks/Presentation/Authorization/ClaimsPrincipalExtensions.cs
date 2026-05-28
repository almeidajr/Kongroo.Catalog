using System.Security.Claims;
using Kongroo.BuildingBlocks.Domain.Exceptions;

namespace Kongroo.BuildingBlocks.Presentation.Authorization;

public static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal user)
    {
        public Guid GetUserId()
        {
            var subject = user.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(subject, out var userId)
                ? userId
                : throw new UnauthorizedException(nameof(ClaimsPrincipal), "missing or invalid subject claim");
        }
    }
}
