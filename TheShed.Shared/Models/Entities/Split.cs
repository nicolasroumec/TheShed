using TheShed.Shared.Models.Base;

namespace TheShed.Shared.Models.Entities
{
    /// <summary>
    /// Parcial de tiempo en una distancia específica.
    ///
    /// Patrón Long Course (piscina 50m) — repite cada 50m:
    ///   15, 25, 35, 45, 50 | 65, 75, 85, 95, 100 | ...
    ///
    /// Patrón Short Course (piscina 25m) — repite cada 50m:
    ///   15, 20, 25, 35, 40, 45, 50 | 65, 70, 75, 85, 90, 95, 100 | ...
    ///   Nota: dentro de cada bloque de 50m, la primera medición post-viraje
    ///   (ej. 35m, 85m) es a 10m del viraje anterior.
    ///   Si el nadador no surfaceó en esos 10m, CycleCount = NULL
    ///   y se usa el BreakoutDistance/BreakoutTime del TurnAnalysis correspondiente.
    /// </summary>
    public class Split : AuditableEntity
    {
        public int Id { get; set; }

        public int SwimPerformanceId { get; set; }
        public SwimPerformance SwimPerformance { get; set; } = null!;

        /// <summary>
        /// Distancia del split (metros).
        /// </summary>
        public int DistanceMeters { get; set; }

        /// <summary>
        /// Tiempo acumulado desde la salida hasta esta distancia (segundos).
        /// </summary>
        public float TimeSeconds { get; set; }

        /// <summary>
        /// Velocidad promedio hasta esta distancia (m/s).
        /// Calculado: DistanceMeters / TimeSeconds.
        /// </summary>
        public float Speed { get; set; }

        /// <summary>
        /// Número de ciclos (brazadas) hasta esta distancia.
        /// NULL en el split de 15m (fase de deslizamiento post-salida).
        /// NULL en primeros splits post-viraje cuando el nadador no ha surfaceado aún
        /// (en LC: split a 15m del viraje; en SC: split a 10m del viraje).
        /// </summary>
        public int? CycleCount { get; set; }

        /// <summary>
        /// Largo de brazada promedio (metros/ciclo).
        /// Calculado: DistanceMeters / CycleCount.
        /// NULL si CycleCount es NULL.
        /// </summary>
        public float? StrokeLength { get; set; }

        /// <summary>
        /// Frecuencia de brazada (ciclos/minuto).
        /// Calculado: (CycleCount / TimeSeconds) * 60.
        /// NULL si CycleCount es NULL.
        /// </summary>
        public float? StrokeRate { get; set; }

        /// <summary>
        /// Índice de brazada — métrica de eficiencia.
        /// Calculado: Speed * StrokeLength.
        /// NULL si StrokeLength es NULL.
        /// </summary>
        public float? StrokeIndex { get; set; }
    }
}
