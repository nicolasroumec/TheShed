using TheShed.Shared.Models.Enums;

namespace TheShed.Shared.Models.DTOs.Vaults
{
    /// <summary>A user who shares a vault, with their role. Returned to the owner's
    /// members panel. The owner is not listed here (they are not a VaultMember).</summary>
    public class VaultMemberItem
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public VaultRole Role { get; set; }
    }
}
