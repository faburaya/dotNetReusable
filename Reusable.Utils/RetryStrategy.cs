using System;

namespace Reusable.Utils
{
    /// <summary>
    /// Gewährt eine Strategie für Wiederholungen von gescheiterten Operationen.
    /// </summary>
    public class RetryStrategy
    {
        private static readonly Random randomGenerator = new Random(DateTime.Now.Millisecond);

        public static TimeSpan CalculateExponentialBackoff(TimeSpan timeSlot, uint attempt)
        {
            lock (randomGenerator)
            {
                return randomGenerator.Next(1 << (int)attempt) * timeSlot;
            }
        }
    }
}
