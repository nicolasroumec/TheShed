using TheShed.Shared.Helpers;

namespace TheShed.Shared.Models.DTOs.Health
{
    /// <summary>Health verdict for one entry: strength rating + reuse flag. Never carries
    /// the plaintext password.</summary>
    public class PasswordHealthItem
    {
        public int EntryId { get; set; }
        public int VaultId { get; set; }
        public string EntryName { get; set; } = string.Empty;
        public PasswordStrength Strength { get; set; }
        public bool IsReused { get; set; }
    }
}
