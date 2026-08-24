using System.ComponentModel.DataAnnotations;

namespace TheShed.Shared.Models.DTOs.Entries
{
    /// <summary>Data to edit an existing entry. Does not include VaultId: an entry
    /// does not change vault when edited. The password replaces the previous one.</summary>
    public class EntryUpdateRequest
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Username { get; set; } = string.Empty;

        // AES-256-GCM ciphertext (vault key), encrypted client-side — the server never sees the
        // plaintext (Sprint 26). Re-encrypting the same plaintext still produces a different
        // blob every time (random nonce per operation), so byte comparison can't tell whether
        // the password actually changed; PasswordChanged carries that instead.
        [Required]
        public string Password { get; set; } = string.Empty;

        // Whether Password differs from the entry's current one — computed client-side, where
        // both plaintexts are available, before either gets encrypted. Drives whether the
        // outgoing password gets snapshotted to EntryHistory.
        public bool PasswordChanged { get; set; }

        [MaxLength(2048)]
        public string? Url { get; set; }

        public string? Notes { get; set; }

        public bool IsFavorite { get; set; }
    }
}
