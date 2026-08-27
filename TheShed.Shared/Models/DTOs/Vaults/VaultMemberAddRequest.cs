using System.ComponentModel.DataAnnotations;
using TheShed.Shared.Models.Enums;

namespace TheShed.Shared.Models.DTOs.Vaults
{
    /// <summary>Share a vault with another user, identified by email, at a given role.</summary>
    public class VaultMemberAddRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public VaultRole Role { get; set; } = VaultRole.Viewer;

        // Generated client-side (RSA-OAEP-wrapped vault key for the target's public key, Sprint
        // 27) — no [Required] here so the EditForm's validator doesn't block submission before
        // it's filled in. VaultsController rejects a request that reaches it empty.
        public string VaultKeyWrap { get; set; } = string.Empty;
    }
}
