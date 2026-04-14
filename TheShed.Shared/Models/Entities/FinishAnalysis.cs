using TheShed.Shared.Models.Base;

namespace TheShed.Shared.Models.Entities
{
    /// <summary>
    /// Análisis de los últimos 5 metros y toque final.
    /// Métricas de brazada + velocidad + tiempo en ese segmento.
    /// </summary>
    public class FinishAnalysis : AuditableEntity
    {
        public int Id { get; set; }

        public int SwimPerformanceId { get; set; }
        public SwimPerformance SwimPerformance { get; set; } = null!;

        /// <summary>
        /// Tiempo de los últimos 5 metros (segundos).
        /// </summary>
        public float? Last5mTime { get; set; }

        /// <summary>
        /// Velocidad de nado en los últimos 5 metros (m/s).
        /// Calculado: 5 / Last5mTime.
        /// </summary>
        public float? Last5mSpeed { get; set; }

        /// <summary>
        /// Número de ciclos en los últimos 5 metros.
        /// NULL si el nadador estaba en deslizamiento final.
        /// </summary>
        public int? CycleCount { get; set; }

        /// <summary>
        /// Largo de brazada en los últimos 5 metros (metros/ciclo).
        /// Calculado: 5 / CycleCount.
        /// NULL si CycleCount es NULL.
        /// </summary>
        public float? StrokeLength { get; set; }

        /// <summary>
        /// Frecuencia de brazada en los últimos 5 metros (ciclos/minuto).
        /// Calculado: (CycleCount / Last5mTime) * 60.
        /// NULL si CycleCount es NULL.
        /// </summary>
        public float? StrokeRate { get; set; }

        /// <summary>
        /// Índice de brazada en los últimos 5 metros — métrica de eficiencia.
        /// Calculado: Last5mSpeed * StrokeLength.
        /// NULL si StrokeLength es NULL.
        /// </summary>
        public float? StrokeIndex { get; set; }
    }
}
