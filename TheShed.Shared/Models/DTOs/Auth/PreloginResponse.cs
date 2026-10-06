namespace TheShed.Shared.Models.DTOs.Auth
{
    /// <summary>The account's KDF salt, fetched before login so the client can derive the
    /// stretched master key — and from it the auth hash — without ever sending the master
    /// password (N1 in FEATURES-ROADMAP.md). Unknown emails get a fake but stable salt.</summary>
    public record PreloginResponse(string KeySalt);
}
