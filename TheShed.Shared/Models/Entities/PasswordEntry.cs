using TheShed.Shared.Models.Base;

namespace TheShed.Shared.Models.Entities
{
    public class PasswordEntry : AuditableEntity
    {
        public int Id { get; set; }
        public int VaultId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordEncrypted { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;  
        public bool IsFavorite { get; set; } = false;
    }
}
