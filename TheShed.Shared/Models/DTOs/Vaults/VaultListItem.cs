using TheShed.Shared.Models.Enums;

namespace TheShed.Shared.Models.DTOs.Vaults
{
    /// <summary>Lightweight vault shape for listings, with the caller's role on it.</summary>
    public class VaultListItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public VaultRole Role { get; set; }
    }
}
