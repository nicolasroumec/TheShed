namespace TheShed.Shared.Models.DTOs.Auth
{
    /// <summary>CSRF token (A4) the client echoes back in the X-CSRF-TOKEN header on every
    /// mutating request.</summary>
    public record AntiforgeryTokenResponse(string Token);
}
