using TheShed.Shared.Models.Base;

namespace TheShed.Shared.Models.Entities
{
    public class User : AuditableEntity
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        // Zero-knowledge key derivation (Sprint 25). Public, non-secret PBKDF2 salt used to
        // re-derive the client-side stretched master key on every device/session. Nullable
        // because users registered before this sprint have none until they log in post-Sprint 28
        // and one gets generated retroactively.
        public string? KeySalt { get; set; }

        // Keypair for zero-knowledge vault sharing (Sprint 27). Same nullability reasoning as
        // KeySalt. PublicKey is PEM text; EncryptedPrivateKey is an opaque base64 blob — the
        // server never holds the private key in the clear.
        public string? PublicKey { get; set; }
        public string? EncryptedPrivateKey { get; set; }

        // Session revocation (N3). Copied into every JWT as the "tv" claim and compared on each
        // request; bumping it invalidates every token issued before (sign out everywhere,
        // master password change).
        public int TokenVersion { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
        public bool TwoFactorEnabled { get; set; } = false;
        // Base64 of the raw TOTP secret. Set (with TwoFactorEnabled still false) while setup is
        // pending confirmation; cleared on disable.
        // ponytail: stored unencrypted — a DB leak lets an attacker mint codes, but they still
        // need the master password and vault data stays client-encrypted. Wrap it with
        // ASP.NET Data Protection once a persistent key ring exists in deployment.
        public string? TwoFactorSecret { get; set; }

        // Last TOTP step accepted, so a code can't be used twice (RFC 6238 §5.2).
        public long? TwoFactorLastStep { get; set; }

        public ICollection<Vault> Vaults { get; set; } = [];
        public ICollection<VaultMember> VaultMemberships { get; set; } = [];
        public ICollection<Tag> Tags { get; set; } = [];
    }
}
