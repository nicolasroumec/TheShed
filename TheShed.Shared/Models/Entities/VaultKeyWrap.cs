using TheShed.Shared.Models.Base;

namespace TheShed.Shared.Models.Entities
{
    /// <summary>
    /// A vault's AES-256 encryption key ("vault key"), wrapped for one user so only they can
    /// unwrap it (Sprint 26). One row per user with access to the vault — today just the owner,
    /// wrapped with their stretched master key; Sprint 27 adds one row per shared member,
    /// wrapped with that member's RSA public key instead. The server only ever stores and
    /// returns <see cref="WrappedKey"/> as an opaque blob — it cannot unwrap it.
    /// </summary>
    public class VaultKeyWrap : AuditableEntity
    {
        public int Id { get; set; }
        public int VaultId { get; set; }
        public int UserId { get; set; }
        public string WrappedKey { get; set; } = string.Empty;

        public Vault Vault { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
