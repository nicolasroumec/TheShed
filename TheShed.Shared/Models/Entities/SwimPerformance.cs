using System.ComponentModel.DataAnnotations;
using TheShed.Shared.Models.Base;
using TheShed.Shared.Models.Enums;

namespace TheShed.Shared.Models.Entities
{
    /// <summary>
    /// Representa una carrera específica de un nadador (video + análisis completo).
    /// Puede ser competición oficial o entrenamiento.
    /// </summary>
    public class SwimPerformance : AuditableEntity
    {
        public int Id { get; set; }

        public int SwimmerId { get; set; }
        public User Swimmer { get; set; } = null!;

        public SessionType SessionType { get; set; } = SessionType.Competition;

        /// <summary>
        /// NULL si es entrenamiento
        /// </summary>
        public int? CompetitionId { get; set; }
        public Competition? Competition { get; set; }

        public int EventId { get; set; }
        public Event Event { get; set; } = null!;

        /// <summary>
        /// Carril (1-10). NULL en entrenamientos.
        /// </summary>
        [Range(1, 10)]
        public int? Lane { get; set; }

        /// <summary>
        /// Tiempo oficial en segundos. NULL hasta completar el análisis.
        /// </summary>
        public float? OfficialTimeSeconds { get; set; }

        public VideoSourceType VideoType { get; set; } = VideoSourceType.Upload;

        [Required]
        [MaxLength(500)]
        public string VideoUrl { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; }

        public AnalysisStatus Status { get; set; } = AnalysisStatus.Pending;

        /// <summary>
        /// NULL = no asignado aún
        /// </summary>
        public int? AnalystId { get; set; }
        public User? Analyst { get; set; }

        /// <summary>
        /// NULL = análisis no completado
        /// </summary>
        public DateTime? AnalyzedAt { get; set; }

        [MaxLength(2000)]
        public string? AnalysisNotes { get; set; }

        // Navigation
        public StartAnalysis? StartAnalysis { get; set; }
        public ICollection<TurnAnalysis> TurnAnalyses { get; set; } = new List<TurnAnalysis>();
        public ICollection<Split> Splits { get; set; } = new List<Split>();
        public FinishAnalysis? FinishAnalysis { get; set; }
        public ICollection<AnalystComment> Comments { get; set; } = new List<AnalystComment>();
    }
}
