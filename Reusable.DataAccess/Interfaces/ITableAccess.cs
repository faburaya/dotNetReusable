
namespace Reusable.DataAccess
{
    /// <summary>
    /// Schnittstelle für Ausfüllung der Datenbank mit ursprünglichen Daten.
    /// </summary>
    /// <typeparam name="DataType">Der Datentype auszufüllen.</typeparam>
    public interface ITableAccess<DataType>
    {
        void Insert(DataType obj);

        bool IsEmpty();

        void Commit();
    }
}