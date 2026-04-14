using TheShed.Shared.Models.Base;

namespace TheShed.Shared.Models.Entities
{
    /// <summary>
    /// Análisis de un viraje específico.
    /// Cantidad de virajes según distancia y piscina:
    ///   200m LC: 3 virajes (en 50m, 100m, 150m)
    ///   200m SC: 7 virajes (en 25m, 50m, 75m, 100m, 125m, 150m, 175m)
    ///
    /// Referencia del Excel: "Turn N: Breakout Distance/Time/Speed"
    /// (se distingue de la salida usando "Breakout Turn Time")
    /// </summary>
    public class TurnAnalysis : AuditableEntity
    {
        public int Id { get; set; }

        public int SwimPerformanceId { get; set; }
        public SwimPerformance SwimPerformance { get; set; } = null!;

        /// <summary>
        /// Distancia a la que ocurre el viraje (metros).
        /// Ejemplos: 200m LC → 50, 100, 150 | 200m SC → 25, 50, 75, 100, 125, 150, 175
        /// </summary>
        public int TurnDistanceMeters { get; set; }

        /// <summary>
        /// Velocidad de aproximación al viraje (m/s).
        /// Medida en los últimos metros antes de tocar la pared.
        /// </summary>
        public float? ApproachSpeed { get; set; }

        /// <summary>
        /// Distancia de breakout después del viraje (metros).
        /// Desde la pared hasta que la cabeza rompe superficie.
        /// Referencia: "Breakout Turn Time" del Excel (distingue de "Breakout Start Time").
        /// En SC: la línea de referencia es a los 10m del viraje. Puede ser hasta 15m
        /// si el nadador no surfaceó antes; en ese caso usar BreakoutDistance/BreakoutTime.
        /// </summary>
        public float? BreakoutDistance { get; set; }

        /// <summary>
        /// Tiempo bajo el agua después del viraje (segundos).
        /// Referencia: "Breakout Turn Time" del Excel.
        /// </summary>
        public float? BreakoutTime { get; set; }

        /// <summary>
        /// Velocidad promedio durante el breakout (m/s).
        /// Calculado: BreakoutDistance / BreakoutTime.
        /// </summary>
        public float? BreakoutSpeed { get; set; }

        /// <summary>
        /// Tiempo total del viraje (segundos).
        /// Medido desde los últimos 5m antes de la pared hasta los primeros 15m del siguiente largo.
        /// Ejemplo LC 200m (viraje en 50m): tiempo(65m) - tiempo(45m).
        /// Ejemplo SC 200m (viraje en 25m): tiempo(40m) - tiempo(20m).
        /// </summary>
        public float? TotalTurnTime { get; set; }
    }
}
