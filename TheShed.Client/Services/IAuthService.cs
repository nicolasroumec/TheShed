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

        /// <summary>
        /// Re-derives the stretched master key for a session whose cookie is still valid but
        /// whose in-memory keys are gone — a page reload, or the idle lock (Sprint 30). Does not
        /// touch the JWT cookie: locking is not signing out.
        /// </summary>
        Task<AuthResult> UnlockAsync(string masterPassword);

        /// <summary>
        /// Drops every decryption key this session holds, leaving the JWT cookie in place, so the
        /// next render lands on the unlock prompt. Called by the idle timer (Sprint 30) and by
        /// <see cref="LogoutAsync"/>, which needs the same clearing before it ends the session.
        /// </summary>
        void Lock();

        Task LogoutAsync();
    }
}
