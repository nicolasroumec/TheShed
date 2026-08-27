using TheShed.Server.Enums;
using TheShed.Shared.Models.DTOs.Auth;

namespace TheShed.Server.Services
{
    public record AuthResult(bool Success, AuthError Error, AuthResponse? Response, string? Token = null);

    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
        Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default);

        /// <summary>The current user's own keypair (Sprint 27), for Me() — too large to carry
        /// as JWT claims, unlike KeySalt. (null, null) if the user no longer exists.</summary>
        Task<(string? PublicKey, string? EncryptedPrivateKey)> GetKeypairAsync(int userId, CancellationToken ct = default);
    }
}
