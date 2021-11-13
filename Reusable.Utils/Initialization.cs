using System.Collections.Generic;

namespace Reusable.Utils
{
    /// <summary>
    /// Gewährt einfache Implementierungen für übliche Aufgaben.
    /// </summary>
    /// <typeparam name="DataType">Der Datentyp zu nutzen.</typeparam>
    public static class With<DataType>
    {
        /// <summary>
        /// Signatur für eine Rückrufaktion, die für die Erstellung eines Objektes zuständig ist.
        /// </summary>
        /// <returns>Ein neu erstelltes Objekt.</returns>
        public delegate DataType CreateObject();

        /// <summary>
        /// Erstellt ein Array von Objekten des vorgegebenen Typs.
        /// </summary>
        /// <param name="count">Wie viele Objekte erstellt werden müssen.</param>
        /// <param name="callback">Die Rückrufaktion für die Erstellung der Objekte.</param>
        /// <returns>Ein Array mit <paramref name="count"/> Objekten.</returns>
        public static DataType[] CreateArrayOf(int count, CreateObject callback)
        {
            var array = new DataType[count];
            for (int idx = 0; idx < count; ++idx)
            {
                array[idx] = callback();
            }
            return array;
        }

        /// <summary>
        /// Erstellt eine Liste von Objekten des vorgegebenen Typs.
        /// </summary>
        /// <param name="count">Wie viele Objekte erstellt werden müssen.</param>
        /// <param name="callback">Die Rückrufaktion für die Erstellung der Objekte.</param>
        /// <returns>Ein Liste mit <paramref name="count"/> Objekten.</returns>
        public static List<DataType> CreateListOf(int count, CreateObject callback)
        {
            var list = new List<DataType>(capacity: count);
            for (int idx = 0; idx < count; ++idx)
            {
                list.Add(callback());
            }
            return list;
        }
    }

}// end of namespace Reusable.Utils