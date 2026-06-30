using TheShed.Shared.Models.Enums;

namespace TheShed.Shared.Models.DTOs.Vaults
{
    /// <summary>Change an existing member's role on a vault.</summary>
    public class VaultMemberRoleUpdateRequest
    {
        public VaultRole Role { get; set; }
    }
}
