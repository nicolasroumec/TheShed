using TheShed.Server.Enums;
using TheShed.Shared.Models.DTOs.Auth;

namespace TheShed.Server.Services
{
    public record AuthResult(bool Success, AuthError Error, AuthResponse? Response, string? Token = null);

    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
        Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default);
    }
}
