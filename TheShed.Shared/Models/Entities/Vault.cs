using TheShed.Shared.Models.Base;

namespace TheShed.Shared.Models.Entities
{
    public class Vault : AuditableEntity
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public User Owner { get; set; } = null!;
        public ICollection<PasswordEntry> PasswordEntries { get; set; } = [];
        public ICollection<SecureNote> SecureNotes { get; set; } = [];
        public ICollection<VaultMember> Members { get; set; } = [];
    }
}
