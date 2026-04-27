using TheShed.Shared.Models.Base;

namespace TheShed.Shared.Models.Entities
{
    public class PasswordEntryTag
    {
        public int Id { get; set; }
        public int PasswordEntryId { get; set; }
        public int TagId { get; set; }

        public PasswordEntry? PasswordEntry { get; set; }
        public Tag Tag { get; set; } = null!;
    }
}
