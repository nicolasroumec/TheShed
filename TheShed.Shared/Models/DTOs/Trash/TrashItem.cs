using TheShed.Shared.Models.Enums;

namespace TheShed.Shared.Models.DTOs.Trash
{
    /// <summary>A soft-deleted entry, note or vault. <see cref="VaultId"/>/<see cref="VaultName"/>
    /// are null when <see cref="Type"/> is Vault (the item itself is the vault).</summary>
    public class TrashItem
    {
        public int Id { get; set; }
        public TrashItemType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? VaultId { get; set; }
        public string? VaultName { get; set; }
        public DateTime DeletedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
