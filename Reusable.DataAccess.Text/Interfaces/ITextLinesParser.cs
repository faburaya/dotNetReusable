
namespace Reusable.DataAccess.Text
{
    /// <summary>
    /// Schnittstelle für die Zergliderung von Textinhalt, der in Zeilen organisiert ist.
    /// </summary>
    /// <typeparam name="DataType"></typeparam>
    public interface ITextLinesParser<out DataType>
    {
        /// <summary>
        /// Zergliedert den Textinhalt.
        /// </summary>
        /// <param name="lines">Die Textzeilen.</param>
        /// <returns>Eine Liste von Objekten des Typs <typeparamref name="DataType"/>, dia den Textzeilen entnommen wurden.</returns>
        /// <exception cref="Common.ParserException">Tritt bei einem Fehler der Zergliederung auf.</exception>
        /// <exception cref="AggregateException">Gruppiert mehrere Fehler des Typs <see cref="Common.ParserException"/>.</exception>
        IEnumerable<DataType> Parse(IList<string> lines);
    }
}