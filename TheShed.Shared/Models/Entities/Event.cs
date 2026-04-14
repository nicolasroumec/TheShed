using System.ComponentModel.DataAnnotations;
using TheShed.Shared.Models.Enums;

namespace TheShed.Shared.Models.Entities
{
    /// <summary>
    /// Catálogo de eventos oficiales (estilo + distancia + género).
    /// Ejemplo: 200m Freestyle Masculino.
    /// Distancias válidas por estilo:
    ///   Freestyle:         50, 100, 200, 400, 800, 1500
    ///   Backstroke:        50, 100, 200
    ///   Breaststroke:      50, 100, 200
    ///   Butterfly:         50, 100, 200
    ///   IndividualMedley:  200, 400
    /// </summary>
    public class Event
    {
        public int Id { get; set; }

        public Stroke Stroke { get; set; }

        /// <summary>
        /// Distancia en metros: 50, 100, 200, 400, 800, 1500
        /// </summary>
        [Range(50, 1500)]
        public int DistanceMeters { get; set; }

        public Gender Gender { get; set; }

        // Navigation
        public ICollection<SwimPerformance> Performances { get; set; } = new List<SwimPerformance>();
    }
}
