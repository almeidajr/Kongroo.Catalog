using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Kongroo.Catalog.Specs.Support;

public static class SpecsJwt
{
    public const string Issuer = "Kongroo.Catalog.Specs";
    public const string Audience = "Kongroo.Catalog.Specs";
    public const string SigningKey = "Kongroo.Catalog.Specs.SigningKey.For.Bdd.Tests";

    public static string CreateToken(Guid userId, string username, string role)
    {
        var handler = new JwtSecurityTokenHandler();
        var issuedAt = DateTime.UtcNow;
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                new Claim(ClaimTypes.Role, role),
            ]),
            Issuer = Issuer,
            Audience = Audience,
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = issuedAt.AddMinutes(15),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                SecurityAlgorithms.HmacSha256
            ),
        };

        var securityToken = handler.CreateToken(tokenDescriptor);

        return handler.WriteToken(securityToken);
    }
}
