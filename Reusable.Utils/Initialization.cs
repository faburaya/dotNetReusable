using System.Collections.Generic;

namespace Reusable.Utils
{
    /// <summary>
    /// Gewährt einfache Implementierungen für übliche Aufgaben.
    /// </summary>
    /// <typeparam name="DataType">Der Datentyp zu nutzen.</typeparam>
    public static class With<DataType>
    {
        public delegate DataType CreateObject();

        public static DataType[] CreateArrayOf(int count, CreateObject callback)
        {
            var array = new DataType[count];
            for (int idx = 0; idx < count; ++idx)
            {
                array[idx] = callback();
            }
            return array;
        }

        public static List<DataType> CreateListOf(int count, CreateObject callback)
        {
            var list = new List<DataType>(capacity: count);
            for (int idx = 0; idx < count; ++idx)
            {
                list.Add(callback());
            }
            return list;
        }

    }// end of class With

}// end of namespace Reusable.Utils