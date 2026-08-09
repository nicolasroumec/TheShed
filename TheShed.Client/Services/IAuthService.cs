using TheShed.Shared.Models.DTOs.Auth;

namespace TheShed.Client.Services
{
    /// <summary>Outcome of an auth operation. It carries no user data on purpose: after a
    /// success the caller reads the signed-in user from AuthenticationStateProvider, which is
    /// the single source of truth.</summary>
    public record AuthResult(bool Success, string? Error)
    {
        public static AuthResult Ok() => new(true, null);
        public static AuthResult Fail(string error) => new(false, error);
    }

    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(LoginRequest request);
        Task<AuthResult> RegisterAsync(RegisterRequest request);
        Task LogoutAsync();
    }
}
