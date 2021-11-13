using System;

namespace Reusable.Utils
{
    /// <summary>
    /// Gewährt eine Strategie für Wiederholungen von gescheiterten Operationen.
    /// </summary>
    public static class RetryStrategy
    {
        private static readonly Random randomGenerator = new Random(DateTime.Now.Millisecond);

        /// <summary>
        /// Rechnet die Wartezeit für die Wiederholung einer Operation.
        /// </summary>
        /// <param name="timeSlot">Die minimale Wartezeit.</param>
        /// <param name="attempt">Die bisherige Anzahl von Wiederholungen.</param>
        /// <returns>Die gerechnete Wartezeit.</returns>
        public static TimeSpan CalculateExponentialBackoff(TimeSpan timeSlot, uint attempt)
        {
            lock (randomGenerator)
            {
                return randomGenerator.Next(1 << (int)attempt) * timeSlot;
            }
        }
    }
}
