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
    }
}
