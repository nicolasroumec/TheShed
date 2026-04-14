using TheShed.Shared.Models.Base;

namespace TheShed.Shared.Models.Entities
{
    /// <summary>
    /// Análisis de la salida (desde el pitido hasta el breakout inicial).
    /// Referencia del Excel: "Breakout Start Time" = tiempo del breakout de salida.
    /// </summary>
    public class StartAnalysis : AuditableEntity
    {
        public int Id { get; set; }

        public int SwimPerformanceId { get; set; }
        public SwimPerformance SwimPerformance { get; set; } = null!;

        /// <summary>
        /// Tiempo de reacción (segundos): desde el pitido hasta el primer movimiento.
        /// </summary>
        public float? ReactionTime { get; set; }

        /// <summary>
        /// Tiempo de despegue del pie delantero (segundos).
        /// Solo aplica en salidas de frente (Freestyle, Butterfly, Breaststroke).
        /// NULL para Backstroke.
        /// </summary>
        public float? TakeOffFrontFootTime { get; set; }

        /// <summary>
        /// Tiempo de despegue del pie trasero (segundos).
        /// Solo aplica en salidas de frente (Freestyle, Butterfly, Breaststroke).
        /// NULL para Backstroke.
        /// </summary>
        public float? TakeOffRearFootTime { get; set; }

        /// <summary>
        /// Tiempo en que los pies se despegan de la pared (segundos).
        /// Solo aplica en Backstroke (salida desde el agua).
        /// NULL para otros estilos.
        /// </summary>
        public float? TakeOffFeetPushOffTime { get; set; }

        /// <summary>
        /// Tiempo de despegue de las manos del bloque (segundos).
        /// </summary>
        public float? TakeOffHandTime { get; set; }

        /// <summary>
        /// Tiempo en el aire (segundos). Solo aplica en salidas de frente.
        /// NULL para Backstroke. Métricamente opcional según el contexto de análisis.
        /// </summary>
        public float? FlyTime { get; set; }

        /// <summary>
        /// Distancia de breakout (metros): desde la pared hasta que la cabeza rompe superficie.
        /// Referencia: "Breakout Distance" del Excel.
        /// </summary>
        public float? BreakoutDistance { get; set; }

        /// <summary>
        /// Tiempo de breakout (segundos): desde la salida hasta que la cabeza rompe superficie.
        /// Referencia: "Breakout Start Time" del Excel.
        /// </summary>
        public float? BreakoutTime { get; set; }

        /// <summary>
        /// Velocidad durante el breakout (m/s).
        /// Calculado: BreakoutDistance / BreakoutTime.
        /// </summary>
        public float? BreakoutSpeed { get; set; }
    }
}
