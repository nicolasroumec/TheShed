using TheShed.Shared.Models.DTOs.Auth;

namespace TheShed.Client.Services
{
    /// <summary>Result of an auth operation, carrying either the response or an error message.</summary>
    public record AuthResult(bool Success, AuthResponse? Response, string? Error)
    {
        public static AuthResult Ok(AuthResponse response) => new(true, response, null);
        public static AuthResult Fail(string error) => new(false, null, error);
    }

    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(LoginRequest request);
        Task<AuthResult> RegisterAsync(RegisterRequest request);
        Task LogoutAsync();
    }
}
