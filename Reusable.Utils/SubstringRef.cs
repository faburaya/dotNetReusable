using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Reusable.Utils
{
    /// <summary>
    /// Stellt ein Verweis auf eine Zeichenkette innerhalb einem bestimmten Text.
    /// </summary>
    public readonly struct SubstringRef : IComparable<SubstringRef>
    {
        private readonly string source;

        /// <summary>
        /// Der Index des Anfangs der Zeichenkette.
        /// </summary>
        public readonly int start;

        /// <summary>
        /// Die Länge der Zeichenkette.
        /// </summary>
        public readonly int length;

        /// <summary>
        /// Erstellt ein leeres Objekt.
        /// </summary>
        public SubstringRef()
        {
            source = string.Empty;
            start = 0;
            length = 0;
        }

        /// <summary>
        /// Erstellt ein Objekt des Typs <see cref="SubstringRef"/>.
        /// </summary>
        /// <param name="source">Der Quelltext.</param>
        /// <param name="start">Der Index des Anfangs der Zeichenkette.</param>
        /// <param name="length">Die Länge der Zeichenkette.</param>
        public SubstringRef(string source, int start, int length)
        {
            Debug.Assert(source != null);
            Debug.Assert(length <= source.Length - start);
            this.source = source;
            this.start = start;
            this.length = length;
        }

        /// <summary>
        /// Erstellt ein Objekt des Typs <see cref="SubstringRef"/>.
        /// </summary>
        /// <param name="match">Eine einzelne Übereinstimmung eines regulären Ausdruck im Quelltext.</param>
        /// <param name="source">Der Quelltext.</param>
        public SubstringRef(Capture match, string source)
            : this(source, match.Index, match.Length)
        {
        }

        /// <summary>
        /// Wandelt das Objekt in den Typ <see cref="ReadOnlySpan{T}"/> um.
        /// </summary>
        /// <returns>Eine Instanz des Typs <see cref="ReadOnlySpan{T}"/>, die auf den Quelltext hinweist.</returns>
        public ReadOnlySpan<char> AsSpan()
        {
            return source.AsSpan(start, length);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return source.Substring(start, length);
        }

        /// <summary>
        /// Vergleicht diese Instanz mit einem anderem Objekt desselben Typs.
        /// </summary>
        /// <param name="other">Das andere Objekt.</param>
        /// <param name="comparison">Wie der Vergleich auszuführen ist.</param>
        /// <returns>
        /// Gibt eine Zahl unter 0 zurück, falls diese Instanz vor <paramref name="other"/> einzuordnen ist.
        /// Gibt eine Zahl über 0 zurück, falls diese Instanz nach <paramref name="other"/> einzuordnen ist.
        /// Gibt 0 zurück, falls der Inhalt dieses Instanz dem Inhalt von <paramref name="other"/> gleicht.
        /// </returns>
        public int CompareTo(SubstringRef other, StringComparison comparison)
        {
            if (source == other.source
                && start == other.start
                && length == other.length)
            {
                return 0;
            }
            return AsSpan().CompareTo(other.AsSpan(), comparison);
        }

        /// <inheritdoc/>
        public int CompareTo(SubstringRef other)
        {
            return CompareTo(other, StringComparison.Ordinal);
        }
    }
}
