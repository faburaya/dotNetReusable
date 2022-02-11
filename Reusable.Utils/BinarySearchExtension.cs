using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Reusable.Utils
{
    /// <summary>
    /// Implementiert eine allgemeine binäre Suche für sortierte Listen.
    /// </summary>
    public static class BinarySearchExtension
    {
        /// <summary>
        /// Findet in der Liste die untere Grenze ("lower bound"):
        /// Das erste Element, das den gegebenen Schlüssel aufweist, wenn es vorhaden ist,
        /// andernfalls ist es das unmittelbar nächste Element.
        /// </summary>
        /// <typeparam name="KeyType">Der Typ des Schlüssels.</typeparam>
        /// <typeparam name="DataType">Der Datentyp.</typeparam>
        /// <param name="values">Die zu suchende Liste.</param>
        /// <param name="key">Der Schlüssel des erwünschten Elements.</param>
        /// <param name="getKeyOf">Gibt den Schlüssel eines gegebenen Elements zurück.</param>
        /// <returns>Der Index der unteren Grenze.</returns>
        public static int SearchLowerBoundIndex<KeyType, DataType>(this IList<DataType> values,
                                                                   KeyType key,
                                                                   Func<DataType, KeyType> getKeyOf)
            where KeyType : IComparable<KeyType>
        {
            int low = 0;
            int high = values.Count;
            int found = BinarySearchImpl(key, values, ref low, ref high, getKeyOf);

            while (low != high)
            {
                high = found;
                found = BinarySearchImpl(key, values, ref low, ref high, getKeyOf);
            }

            return found;
        }

        /// <summary>
        /// Findet in der Liste die obere Grenze ("upper bound"):
        /// Das erste Element, das einen Schlüssel aufweist, das größer als der gegebener Schlüssel ist.
        /// </summary>
        /// <typeparam name="KeyType">Der Typ des Schlüssels.</typeparam>
        /// <typeparam name="DataType">Der Datentyp.</typeparam>
        /// <param name="values">Die zu suchende Liste.</param>
        /// <param name="key">Der Schlüssel des erwünschten Elements.</param>
        /// <param name="getKeyOf">Gibt den Schlüssel eines gegebenen Elements zurück.</param>
        /// <returns>Der Index der oberen Grenze.</returns>
        public static int SearchUpperBoundIndex<KeyType, DataType>(this IList<DataType> values,
                                                                   KeyType key,
                                                                   Func<DataType, KeyType> getKeyOf)
            where KeyType : IComparable<KeyType>
        {
            int low = 0;
            int high = values.Count;
            int found = BinarySearchImpl(key, values, ref low, ref high, getKeyOf);

            while (low != high)
            {
                low = found + 1;
                found = BinarySearchImpl(key, values, ref low, ref high, getKeyOf);
            }

            return found;
        }

        private static int BinarySearchImpl<KeyType, DataType>(KeyType key,
                                                               IList<DataType> values,
                                                               ref int low,
                                                               ref int high,
                                                               Func<DataType, KeyType> getKeyOf)
            where KeyType : IComparable<KeyType>
        {
            Debug.Assert(low <= high);

            while (low < high)
            {
                int middle = (low + high) / 2;

                if (getKeyOf(values[middle]).CompareTo(key) < 0)
                {
                    low = middle + 1;
                }
                else if (getKeyOf(values[middle]).CompareTo(key) > 0)
                {
                    high = middle;
                }
                else
                {
                    return middle;
                }
            }

            return high;
        }
    }
}
