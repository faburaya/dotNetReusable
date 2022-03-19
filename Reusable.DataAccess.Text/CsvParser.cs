using System.Reflection;

using Reusable.Utils;

namespace Reusable.DataAccess.Text
{
    /// <summary>
    /// Zergliedert Textinhalt im CSV-Format.
    /// </summary>
    /// <typeparam name="DataType">Der Datentyp, dessen Instanzen von der Zergliederung erzeugt werden.</typeparam>
    public class CsvParser<DataType> : ITextLinesParser<DataType> where DataType : new()
    {
        private struct ParsingField : IComparable<ParsingField>
        {
            public PropertyInfo property;
            public string columnName;

            public int CompareTo(ParsingField other)
            {
                return columnName.CompareTo(other.columnName);
            }
        }

        private static readonly List<ParsingField> fieldsToParse;

        static CsvParser()
        {
            fieldsToParse = new();

            foreach (PropertyInfo property in typeof(DataType).GetProperties())
            {
                DataModels.FieldAttribute? boundField = property.GetCustomAttribute<DataModels.FieldAttribute>();
                if (boundField == null)
                    continue;

                MethodInfo? setter = property.GetSetMethod();
                if (setter == null || setter.IsPrivate)
                {
                    throw new Common.ParserException($"Der Zergliederung des Typs {typeof(DataType).FullName} kann das Property {property.Name} nicht nutzen, denn es ist schreibgeschützt!");
                }

                fieldsToParse.Add(new ParsingField { property = property, columnName = boundField.Name.ToUpper() });
            }

            fieldsToParse.Sort();
        }

        /// <inheritdoc cref="ITextLinesParser{DataType}.Parse(IList{string})"/>
        public IEnumerable<DataType> Parse(IList<string> lines)
        {
            if (lines.Count == 0)
            {
                throw new Common.ParserException("Die Kopfzeile fehlt!");
            }

            string header = lines[0];
            string[] columnNames = header.Split(',');
            if (columnNames.Length == 0)
            {
                throw new Common.ParserException("Es gibt keine Spalte!");
            }

            (string name, int index, PropertyInfo property)[] columnsToParse =
                (from field in fieldsToParse select (field.columnName, -1, field.property)).ToArray();

            HashSet<string> checkedColumns = new(capacity: columnNames.Length);
            for (int colIdx = 0; colIdx < columnNames.Length; ++colIdx)
            {
                string columnName = columnNames[colIdx].Trim().ToUpper();
                if (!checkedColumns.Add(columnName))
                {
                    throw new Common.ParserException($"Es gibt mehrere Spalten mit dem Namen {columnName}!");
                }

                for (int idx = fieldsToParse.SearchLowerBoundIndex(columnName, field => field.columnName);
                     idx < fieldsToParse.Count && fieldsToParse[idx].columnName == columnName;
                     ++idx)
                {
                    columnsToParse[idx].index = colIdx;
                }
            }

            List<Common.ParserException> errors = new();
            foreach (var column in columnsToParse)
            {
                if (column.index < 0)
                {
                    errors.Add(new Common.ParserException(
                        $"Typ {typeof(DataType).FullName}, Property {column.property.Name}: Kopfzeile enthält keine Spalte '{column.name}'"));
                }
            }

            if (errors.Count > 0)
            {
                throw new AggregateException(
                    "Die Texteingabe fehlt an Spalten, die für die Zergliderung notwendig sind.", errors);
            }

            for (int rowIdx = 1; rowIdx < lines.Count; ++rowIdx)
            {
                DataType mappedObject = new();

                try
                {
                    string[] fields = lines[rowIdx].Split(',');
                    if (fields.Length != columnNames.Length)
                    {
                        throw new Common.ParserException($"Die Zeile #{rowIdx} stimmt nicht mit der Kopfzeile überein: die Anzahl von Spalten sind nicht gleich!");
                    }

                    foreach (var column in columnsToParse)
                    {
                        string textValue = fields[column.index].Trim();

                        try
                        {
                            object parsedValue = Convert.ChangeType(textValue, column.property.PropertyType);
                            column.property.SetValue(mappedObject, parsedValue);
                        }
                        catch (Exception ex)
                        when (ex is FormatException || ex is OverflowException || ex is InvalidCastException)
                        {
                            throw new Common.ParserException($"Zeile #{rowIdx}, Feld '{column.name}', Wert '{textValue}' kann nicht umgewandelt werden: {ex.Message}");
                        }
                    }
                }
                catch (Common.ParserException ex)
                {
                    errors.Add(ex);
                    continue;
                }

                yield return mappedObject;
            }

            if (errors.Count > 0)
            {
                throw new AggregateException("Fehler sind während der Zergliederung aufgetreten.", errors);
            }
        }
    }
}