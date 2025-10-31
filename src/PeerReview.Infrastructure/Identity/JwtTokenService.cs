using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PeerReview.Application.Abstractions;
using PeerReview.Domain.Entities;

namespace PeerReview.Infrastructure.Identity;
public class JwtTokenService : IJwtTokenService
{
    private readonly string _issuer, _audience, _key;
    public JwtTokenService(string issuer, string audience, string key)
    {
        _issuer = issuer; _audience = audience; _key = key;
    }
    public string CreateToken(User user, string roleName, TimeSpan lifetime)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>{
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, roleName)
        };
        var token = new JwtSecurityToken(_issuer, _audience, claims, expires: DateTime.UtcNow.Add(lifetime), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
