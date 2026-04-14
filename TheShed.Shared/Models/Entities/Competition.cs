using System.ComponentModel.DataAnnotations;
using TheShed.Shared.Models.Enums;

namespace TheShed.Shared.Models.Entities
{
    public class Competition
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        [MaxLength(200)]
        public string? Location { get; set; }

        public CompetitionLevel Level { get; set; } = CompetitionLevel.Club;

        [MaxLength(200)]
        public string? Organizer { get; set; }

        public int PoolId { get; set; }
        public Pool Pool { get; set; } = null!;

        // Navigation
        public ICollection<SwimPerformance> Performances { get; set; } = new List<SwimPerformance>();
    }
}
