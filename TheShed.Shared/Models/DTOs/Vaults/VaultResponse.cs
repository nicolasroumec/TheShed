namespace TheShed.Shared.Models.DTOs.Vaults
{
    /// <summary>Detail of a single vault, with the caller's permissions on it.
    /// <see cref="IsOwner"/> gates rename/delete/share; <see cref="CanWrite"/> gates
    /// adding/editing entries (owner or Editor member).</summary>
    public class VaultResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsOwner { get; set; }
        public bool CanWrite { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // The caller's own wrap of this vault's AES key (Sprint 26) — base64
        // nonce||ciphertext||tag, see IVaultKeyService. Null for a shared member until Sprint 27
        // wraps the vault key for them too, via their RSA public key instead of a shared secret.
        public string? WrappedKey { get; set; }
    }
}
