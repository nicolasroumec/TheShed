using TheShed.Server.Enums;
using TheShed.Shared.Models.DTOs.Auth;

namespace TheShed.Server.Services
{
    public record AuthResult(bool Success, AuthError Error, AuthResponse? Response, string? Token = null);

    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
        Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default);

        /// <summary>The account's KeySalt, or — for an unknown email or an account without one —
        /// a fake salt derived from the email, stable across calls and the same length as a real
        /// one, so the endpoint doesn't reveal which emails are registered.</summary>
        Task<string> GetPreloginSaltAsync(string email, CancellationToken ct = default);

        /// <summary>Verifies the current auth hash and swaps in the new hash, salt and wrapped
        /// private key. Returns a fresh token: the old one carries the old keySalt claim.</summary>
        Task<AuthResult> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default);

        /// <summary>The current user's own keypair (Sprint 27), for Me() — too large to carry
        /// as JWT claims, unlike KeySalt. (null, null) if the user no longer exists.</summary>
        Task<(string? PublicKey, string? EncryptedPrivateKey)> GetKeypairAsync(int userId, CancellationToken ct = default);
    }
}
