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
    }
}
