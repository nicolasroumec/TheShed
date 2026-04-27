using TheShed.Shared.Models.Base;
using TheShed.Shared.Models.Enums;

namespace TheShed.Shared.Models.Entities
{
    public class VaultMember : AuditableEntity
    {
        public int Id { get; set; }
        public int VaultId { get; set; }
        public int UserId { get; set; }
        public VaultRole Role { get; set; }
    }
}
