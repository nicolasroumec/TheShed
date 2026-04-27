using TheShed.Shared.Models.Base;

namespace TheShed.Shared.Models.Entities
{
    public class Tag : AuditableEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;

        public User User { get; set; } = null!;
        public ICollection<PasswordEntryTag> PasswordEntries { get; set; } = [];
    }
}
