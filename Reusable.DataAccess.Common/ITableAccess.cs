
namespace Reusable.DataAccess.Common
{
    /// <summary>
    /// Schnittstelle für Ausfüllung der Datenbank mit ursprünglichen Daten.
    /// </summary>
    /// <remarks>
    /// Für eine relationale Datenbank ist die Einheit der Speicherung eine Tabelle,
    /// aber für Azure Cosmos ist es das Container für den gegebenen Datentyp.
    /// </remarks>
    /// <typeparam name="DataType">Der zu ausfüllende Datentyp.</typeparam>
    public interface ITableAccess<in DataType>
    {
        /// <summary>
        /// Fügt eine neue Reihe (Element / Unterlage).
        /// </summary>
        void Insert(DataType obj);

        /// <summary>
        /// Prüft, ob die Tabelle leer ist.
        /// </summary>
        /// <returns></returns>
        bool IsEmpty();

        /// <summary>
        /// Gewährleistet die Speicherung der bisherig gesammelten Objekten.
        /// Die Implementierung ist nicht dazu verpflichtet,
        /// dass die Vorgänge transaktional durchgeführt werden.
        /// </summary>
        void Commit();
    }
}