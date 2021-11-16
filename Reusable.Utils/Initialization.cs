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
            return CreateArrayOf(count, (int index) => callback());
        }

        /// <summary>
        /// Erstellt eine Liste von Objekten des vorgegebenen Typs.
        /// </summary>
        /// <param name="count">Wie viele Objekte erstellt werden müssen.</param>
        /// <param name="callback">Die Rückrufaktion für die Erstellung der Objekte.</param>
        /// <returns>Ein Liste mit <paramref name="count"/> Objekten.</returns>
        public static List<DataType> CreateListOf(int count, CreateObject callback)
        {
            return CreateListOf(count, (int index) => callback());
        }

        /// <summary>
        /// Signatur für eine Rückrufaktion, die für die Erstellung eines Objektes zuständig ist.
        /// </summary>
        /// <param name="index">Der Index des Objekts, das in einer Reihe steht.</param>
        /// <returns>Ein neu esrtelltes Objekt.</returns>
        public delegate DataType CreateObjectAt(int index);

        /// <summary>
        /// Erstellt ein Array von Objekten des vorgegebenen Typs.
        /// </summary>
        /// <param name="count">Wie viele Objekte erstellt werden müssen.</param>
        /// <param name="callback">Die Rückrufaktion für die Erstellung der Objekte, wobei der Index jedes Objektes übergeben wird.</param>
        /// <returns>Ein Array mit <paramref name="count"/> Objekten.</returns>
        public static DataType[] CreateArrayOf(int count, CreateObjectAt callback)
        {
            var array = new DataType[count];
            for (int idx = 0; idx < count; ++idx)
            {
                array[idx] = callback(idx);
            }
            return array;
        }

        /// <summary>
        /// Erstellt eine Liste von Objekten des vorgegebenen Typs.
        /// </summary>
        /// <param name="count">Wie viele Objekte erstellt werden müssen.</param>
        /// <param name="callback">Die Rückrufaktion für die Erstellung der Objekte, wobei der Index jedes Objektes übergeben wird.</param>
        /// <returns>Ein Liste mit <paramref name="count"/> Objekten.</returns>
        public static List<DataType> CreateListOf(int count, CreateObjectAt callback)
        {
            var list = new List<DataType>(capacity: count);
            for (int idx = 0; idx < count; ++idx)
            {
                list.Add(callback(idx));
            }
            return list;
        }

        /// <summary>
        /// Schafft eine einzige Ansicht durch die Zusammensetzung von verschiedenen Sammlungen,
        /// ohne dass deren Inhalt kopiert werden muss.
        /// </summary>
        /// <param name="collections">Die viele zusammenzusetzenden Sammlungen.</param>
        /// <returns>Eine Ansicht, die alle Elemente aller Sammlungen zusammenfasst.</returns>
        public static IEnumerable<DataType> ConcatenateViewOf(IEnumerable<IEnumerable<DataType>> collections)
        {
            foreach (IEnumerable<DataType> collection in collections)
            {
                foreach (DataType item in collection)
                {
                    yield return item;
                }
            }
        }
    }

}// end of namespace Reusable.Utils