using System.ComponentModel.DataAnnotations;

namespace TheShed.Shared.Models.DTOs.Auth
{
    /// <summary>
    /// Master password change (N2 in FEATURES-ROADMAP.md). Same shape as RegisterRequest: the
    /// form binds the raw passwords, but on the wire both are AuthHash.Compute(stretched key),
    /// and the new salt + re-wrapped private key + re-wrapped owned vault keys are attached by
    /// the client right before sending. Shared vaults (wrapped with the RSA public key) and
    /// entries don't change at all.
    /// </summary>
    public class ChangePasswordRequest
    {
        [Required, MaxLength(128)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, MinLength(8), MaxLength(128)]
        public string NewPassword { get; set; } = string.Empty;

        // No [Required], same reason as RegisterRequest.KeySalt: still empty when the EditForm
        // validates. AuthController guards their presence server-side.
        public string NewKeySalt { get; set; } = string.Empty;

        public string NewEncryptedPrivateKey { get; set; } = string.Empty;

        // Every OwnedVaultKey from GET /api/auth/owned-vault-keys, re-wrapped with the new
        // stretched key. The server requires exactly that set (see AuthService.ChangePasswordAsync).
        public List<OwnedVaultKey> VaultKeys { get; set; } = [];
    }
}
