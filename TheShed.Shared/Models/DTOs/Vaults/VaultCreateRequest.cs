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

        // Generated client-side (IVaultKeyService) and attached right before the request goes
        // out — the vault's AES-256 key, wrapped with the owner's stretched master key. Opaque
        // blob to the server. No [Required]: same EditForm-validates-before-population reason
        // as RegisterRequest.KeySalt; VaultsController guards its presence server-side instead.
        public string VaultKeyWrap { get; set; } = string.Empty;
    }
}
