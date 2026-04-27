using TheShed.Shared.Models.Base;

namespace TheShed.Shared.Models.Entities
{
    public class PasswordEntryTag : AuditableEntity
    {
        public int Id { get; set; }
        public int PasswordEntryId { get; set; }
        public int TagId { get; set; }
    }
}
