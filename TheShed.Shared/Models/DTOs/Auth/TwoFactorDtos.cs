using System.ComponentModel.DataAnnotations;

namespace TheShed.Shared.Models.DTOs.Auth
{
    /// <summary>A pending 2FA secret: base32 for manual entry, plus the otpauth:// URI the client
    /// renders as a QR code. Not active until confirmed with a code (TwoFactorCodeRequest).</summary>
    public record TwoFactorSetupResponse(string Secret, string OtpAuthUri);

    public record TwoFactorStatusResponse(bool Enabled);

    /// <summary>A 6-digit authenticator code, to confirm enabling or disabling 2FA.</summary>
    public class TwoFactorCodeRequest
    {
        [Required, StringLength(6, MinimumLength = 6)]
        public string Code { get; set; } = string.Empty;
    }
}
