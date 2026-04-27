using TheShed.Shared.Models.Base;

namespace TheShed.Shared.Models.Entities
{
    public class Vault : AuditableEntity
    {
        public int Id { get; set; }
        public int OwnerId { get; set; } 
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
