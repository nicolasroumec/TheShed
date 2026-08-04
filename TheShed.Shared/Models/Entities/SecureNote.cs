using TheShed.Shared.Models.Base;

namespace TheShed.Shared.Models.Entities
{
    public class SecureNote : AuditableEntity, IVaultScoped
    {
        public int Id { get; set; }
        public int VaultId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ContentEncrypted { get; set; } = string.Empty;
        public bool IsFavorite { get; set; } = false;

        public Vault Vault { get; set; } = null!;
    }
}
