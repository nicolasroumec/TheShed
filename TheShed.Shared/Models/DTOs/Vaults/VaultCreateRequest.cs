using System.ComponentModel.DataAnnotations;

namespace TheShed.Shared.Models.DTOs.Vaults
{
    /// <summary>Data to create a vault. The caller becomes its Owner.</summary>
    public class VaultCreateRequest
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }
    }
}
