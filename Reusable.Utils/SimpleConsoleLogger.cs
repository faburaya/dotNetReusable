using System;

namespace Reusable.Utils
{
    /// <summary>
    /// Bietet einfache Protokollierung, die auf die Konsole schreibt.
    /// </summary>
    public class SimpleConsoleLogger : SimpleLoggerBase
    {
        /// <inheritdoc/>
        protected override void WriteLine(string text)
        {
            Console.WriteLine(text);
        }
    }
}
