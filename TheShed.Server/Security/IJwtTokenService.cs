using TheShed.Shared.Models.Entities;

namespace TheShed.Server.Security
{
    public interface IJwtTokenService
    {
        /// <summary>Genera un access token JWT para el usuario. Devuelve token y vencimiento.</summary>
        (string Token, DateTime ExpiresAt) GenerateToken(User user);
    }
}
