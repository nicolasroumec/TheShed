namespace TheShed.Shared.Models.Helpers
{
    /// <summary>
    /// Utilidades para calcular los splits esperados y virajes según distancia y piscina.
    ///
    /// Patrón Long Course (50m pool) — bloque de 50m:
    ///   offsets: 15, 25, 35, 45, 50
    ///   100m LC → 15, 25, 35, 45, 50, 65, 75, 85, 95, 100
    ///
    /// Patrón Short Course (25m pool) — bloque de 50m (confirmado por analista):
    ///   offsets: 15, 20, 25, 35, 40, 45, 50
    ///   100m SC → 15, 20, 25, 35, 40, 45, 50, 65, 70, 75, 85, 90, 95, 100
    ///   Dentro del bloque, el split a offset 35 (ej. 35m, 85m) está a 10m del viraje
    ///   anterior (en 25m, 75m). Si el nadador no surfaceó en esos 10m, su
    ///   CycleCount es NULL y se consulta TurnAnalysis.BreakoutDistance/BreakoutTime.
    /// </summary>
    public static class SplitConfiguration
    {
        private static readonly int[] LcBlockPattern = { 15, 25, 35, 45, 50 };
        private static readonly int[] ScBlockPattern = { 15, 20, 25, 35, 40, 45, 50 };

        /// <summary>
        /// Retorna las distancias de splits esperadas según distancia de carrera y largo de piscina.
        /// </summary>
        public static List<int> GetExpectedSplits(int raceDistanceMeters, int poolLengthMeters)
        {
            return poolLengthMeters switch
            {
                50 => GetSplitsFromBlockPattern(raceDistanceMeters, LcBlockPattern),
                25 => GetSplitsFromBlockPattern(raceDistanceMeters, ScBlockPattern),
                _ => throw new ArgumentException("El largo de piscina debe ser 25m o 50m.")
            };
        }

        /// <summary>
        /// Retorna las distancias donde ocurren los virajes.
        /// Fórmula: cada múltiplo del largo de piscina hasta (raceDistance - 1).
        /// </summary>
        public static List<int> GetTurnDistances(int raceDistanceMeters, int poolLengthMeters)
        {
            var turns = new List<int>();
            for (int i = poolLengthMeters; i < raceDistanceMeters; i += poolLengthMeters)
                turns.Add(i);
            return turns;
        }

        /// <summary>
        /// Calcula la cantidad de virajes esperados.
        /// </summary>
        public static int GetExpectedTurnCount(int raceDistanceMeters, int poolLengthMeters)
            => (raceDistanceMeters / poolLengthMeters) - 1;

        /// <summary>
        /// Indica si un split es la primera medición después de un viraje (candidato a CycleCount = NULL).
        /// En LC: el split a +15m del viraje (ej. 65m tras viraje en 50m).
        /// En SC: el split a +10m del viraje (ej. 35m tras viraje en 25m).
        /// Si CycleCount es NULL, consultar TurnAnalysis.BreakoutDistance/BreakoutTime.
        /// </summary>
        public static bool IsFirstPostTurnSplit(int splitDistanceMeters, int poolLengthMeters, int raceDistanceMeters)
        {
            int postTurnOffset = poolLengthMeters == 25 ? 10 : 15;
            return GetTurnDistances(raceDistanceMeters, poolLengthMeters)
                .Any(t => splitDistanceMeters == t + postTurnOffset);
        }

        private static List<int> GetSplitsFromBlockPattern(int raceDistance, int[] blockPattern)
        {
            var splits = new List<int>();
            for (int blockStart = 0; blockStart < raceDistance; blockStart += 50)
            {
                foreach (int offset in blockPattern)
                {
                    int distance = blockStart + offset;
                    if (distance <= raceDistance)
                        splits.Add(distance);
                }
            }
            return splits;
        }
    }
}
