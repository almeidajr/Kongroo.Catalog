using System.Security.Claims;
using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.BuildingBlocks.Presentation.Authorization;
using Shouldly;

namespace Kongroo.Catalog.UnitTests.BuildingBlocks.Presentation.Authorization;

public sealed class ClaimsPrincipalExtensionsTests
{
    private const string MappedNameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";

    [Fact]
    public void GetEmail_WhenMappedEmailClaimPresent_ShouldReturnEmail()
    {
        var user = CreatePrincipal(new Claim(ClaimTypes.Email, "ada@example.com"));

        user.GetEmail().ShouldBe("ada@example.com");
    }

    [Fact]
    public void GetEmail_WhenOnlyRawEmailClaimPresent_ShouldReturnEmail()
    {
        var user = CreatePrincipal(new Claim("email", "ada@example.com"));

        user.GetEmail().ShouldBe("ada@example.com");
    }

    [Fact]
    public void GetEmail_WhenMissing_ShouldThrowUnauthorizedException()
    {
        var user = CreatePrincipal(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        var exception = Should.Throw<UnauthorizedException>(user.GetEmail);

        exception.ResourceName.ShouldBe(nameof(ClaimsPrincipal));
    }

    [Fact]
    public void GetCustomerName_WhenRawNameClaimPresent_ShouldPreferDisplayNameOverUsername()
    {
        var user = CreatePrincipal(
            new Claim("name", "Ada Lovelace"),
            new Claim(MappedNameClaimType, "ada_92") // unique_name maps to ClaimTypes.Name
        );

        user.GetCustomerName().ShouldBe("Ada Lovelace");
    }

    [Fact]
    public void GetCustomerName_WhenOnlyMappedNameClaimPresent_ShouldReturnName()
    {
        var user = CreatePrincipal(new Claim(ClaimTypes.Name, "Ada Lovelace"));

        user.GetCustomerName().ShouldBe("Ada Lovelace");
    }

    [Fact]
    public void GetCustomerName_WhenMissing_ShouldThrowUnauthorizedException()
    {
        var user = CreatePrincipal(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        var exception = Should.Throw<UnauthorizedException>(user.GetCustomerName);

        exception.ResourceName.ShouldBe(nameof(ClaimsPrincipal));
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims) => new(new ClaimsIdentity(claims));
}
