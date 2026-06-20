using System.Security.Claims;
using Kongroo.BuildingBlocks.Domain.Exceptions;

namespace Kongroo.BuildingBlocks.Presentation.Authorization;

public static class ClaimsPrincipalExtensions
{
    // Raw JWT claim names, used as a fallback when inbound claim-type mapping is disabled.
    // The JWT "name" claim is the human display name; it is distinct from "unique_name"
    // (the username), which the default inbound map also projects onto ClaimTypes.Name —
    // so the raw "name" claim is preferred over the mapped type.
    private const string EmailClaimType = "email";
    private const string NameClaimType = "name";

    extension(ClaimsPrincipal user)
    {
        public Guid GetUserId()
        {
            var subject = user.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(subject, out var userId)
                ? userId
                : throw new UnauthorizedException(nameof(ClaimsPrincipal), "missing or invalid subject claim");
        }

        public string GetEmail()
        {
            var email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue(EmailClaimType);

            return string.IsNullOrWhiteSpace(email)
                ? throw new UnauthorizedException(nameof(ClaimsPrincipal), "missing or invalid email claim")
                : email;
        }

        public string GetCustomerName()
        {
            var name = user.FindFirstValue(NameClaimType) ?? user.FindFirstValue(ClaimTypes.Name);

            return string.IsNullOrWhiteSpace(name)
                ? throw new UnauthorizedException(nameof(ClaimsPrincipal), "missing or invalid name claim")
                : name;
        }
    }
}
