using System.ComponentModel.DataAnnotations;
using TheShed.Shared.Models.Base;
using TheShed.Shared.Models.Enums;

namespace TheShed.Shared.Models.Entities
{
    /// <summary>
    /// Comentarios del analista para el nadador.
    /// Comunicación unidireccional: analista → nadador.
    /// </summary>
    public class AnalystComment : AuditableEntity
    {
        public int Id { get; set; }

        public int SwimPerformanceId { get; set; }
        public SwimPerformance SwimPerformance { get; set; } = null!;

        [Required]
        [MaxLength(2000)]
        public string Comment { get; set; } = string.Empty;

        public CommentCategory Category { get; set; } = CommentCategory.General;

        /// <summary>
        /// Distancia específica a la que refiere el comentario.
        /// NULL si es un comentario general.
        /// </summary>
        public int? ReferenceDistanceMeters { get; set; }
    }
}
