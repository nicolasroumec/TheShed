using System.ComponentModel.DataAnnotations;

namespace TheShed.Shared.Models.DTOs.Vaults
{
    /// <summary>Data to edit an existing vault (name and description). The vault id
    /// comes from the route, not the body.</summary>
    public class VaultUpdateRequest
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }
    }
}
