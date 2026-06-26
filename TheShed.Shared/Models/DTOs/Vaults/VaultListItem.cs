namespace TheShed.Shared.Models.DTOs.Vaults
{
    /// <summary>Lightweight vault shape for listings, with the caller's permissions on it.
    /// <see cref="IsOwner"/> gates rename/delete/share; <see cref="CanWrite"/> gates
    /// adding/editing entries (owner or Editor member).</summary>
    public class VaultListItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsOwner { get; set; }
        public bool CanWrite { get; set; }
    }
}
