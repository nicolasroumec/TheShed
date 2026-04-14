using System.ComponentModel.DataAnnotations;

namespace TheShed.Shared.Models.Entities
{
    public class Pool
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 25 = Short Course (SCM) | 50 = Long Course (LCM)
        /// </summary>
        [Range(25, 50)]
        public int LengthMeters { get; set; }

        public bool IsIndoor { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [Range(1, 10)]
        public int? LanesCount { get; set; }

        public bool IsOlympic { get; set; } = false;

        // Navigation
        public ICollection<Competition> Competitions { get; set; } = new List<Competition>();
    }
}
