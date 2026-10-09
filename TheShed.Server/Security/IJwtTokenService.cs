using TheShed.Shared.Models.Entities;

namespace TheShed.Server.Security
{
    public interface IJwtTokenService
    {
        /// <summary>Generates a JWT access token for the user. Returns the token and its expiry.</summary>
        (string Token, DateTime ExpiresAt) GenerateToken(User user);
    }
}
