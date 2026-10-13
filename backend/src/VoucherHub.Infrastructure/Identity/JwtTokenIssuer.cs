using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VoucherHub.Application.Identity;
using VoucherHub.Application.Identity.Ports;
using VoucherHub.Domain.Entities;
namespace VoucherHub.Infrastructure.Identity;
public sealed class JwtTokenIssuer(IOptions<JwtOptions> options) : IAccessTokenIssuer
{
    public LoginResult Issue(User user, DateTime now)
    {
        var _jwtOptions = options.Value;
        var expiresAt = now.AddMinutes(_jwtOptions.AccessTokenLifetimeMinutes);
        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256));

        return new LoginResult(
            LoginStatus.Success,
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);

    }
}
