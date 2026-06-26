using TheShed.Shared.Models.Enums;

namespace TheShed.Shared.Models.DTOs.Vaults
{
    /// <summary>Detail of a single vault, including the caller's role on it.</summary>
    public class VaultResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public VaultRole Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
