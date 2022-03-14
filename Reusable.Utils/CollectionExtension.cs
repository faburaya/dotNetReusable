using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Reusable.Utils
{
    /// <summary>
    /// Implementiert Erweiterungen, die bei der Handlung von Sammlungen hilfreich sind.
    /// </summary>
    public static class CollectionExtension
    {
        /// <summary>
        /// Ändert einen Wert in der Hashtabelle durch einen einzigen Vorgang.
        /// </summary>
        /// <typeparam name="KeyType">Der Typ des Schlüsselwerts.</typeparam>
        /// <typeparam name="ValueType">Der Typ des Werts.</typeparam>
        /// <param name="dictionary">Die Hashtabelle.</param>
        /// <param name="key">Der Schlüsselwert.</param>
        /// <param name="getPreviousValueAndSetNew">Eine Rückrufaktion, die den derzeitigen oder vorgegebenen Wert bekommt und einen Neuen setzt.</param>
        /// <returns>Ob ein dem Schlüssel enstsprechender Wert schon vorhanden war.</returns>
        public static bool ChangeValue<KeyType, ValueType>(
            this Dictionary<KeyType, ValueType> dictionary,
            KeyType key,
            Func<ValueType, ValueType> getPreviousValueAndSetNew)
        {
            ref ValueType value = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, key, out bool existed);
            value = getPreviousValueAndSetNew(value);
            return existed;
        }
    }
}
