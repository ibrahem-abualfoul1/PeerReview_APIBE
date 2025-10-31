using PeerReview.Domain.Entities;
namespace PeerReview.Application.Abstractions;
public interface IJwtTokenService
{
    string CreateToken(User user, string roleName, TimeSpan lifetime);
}
