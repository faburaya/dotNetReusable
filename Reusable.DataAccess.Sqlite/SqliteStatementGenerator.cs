using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

using Dapper;

using Reusable.DataModels;

namespace Reusable.DataAccess.Sqlite;

#region Type handlers for Dapper

abstract class SqliteTypeHandler<T> : SqlMapper.TypeHandler<T>
{
    // Parameters are converted by Microsoft.Data.Sqlite
    public override void SetValue(IDbDataParameter parameter, T value)
        => parameter.Value = value;
}

class DateTimeOffsetHandler : SqliteTypeHandler<DateTimeOffset>
{
    public override DateTimeOffset Parse(object value)
        => DateTimeOffset.Parse((string)value);
}

class GuidHandler : SqliteTypeHandler<Guid>
{
    public override Guid Parse(object value)
        => Guid.Parse((string)value);
}

class TimeSpanHandler : SqliteTypeHandler<TimeSpan>
{
    public override TimeSpan Parse(object value)
        => TimeSpan.Parse((string)value);
}

#endregion

/// <summary>
/// Sammelt die Logik zur Erstellung von SQL-Anweisungen.
/// Denn diese Implementierung ist vollständig statisch,
/// muss sie nur einmal per Datentyp ausgeführt werden.
/// </summary>
internal static class SqliteStatementGenerator
{
    private static readonly SortedList<string, string> clrToSqliteType;

    static SqliteStatementGenerator()
    {
        // Aus https://docs.microsoft.com/de-de/dotnet/standard/data/sqlite/types
        clrToSqliteType = new SortedList<string, string> {
            { typeof(bool).Name, "integer" },
            { typeof(byte).Name, "integer" },
            { typeof(byte[]).Name, "blob" },
            { typeof(char).Name, "text" },
            { typeof(DateTime).Name, "text" },
            { typeof(DateTimeOffset).Name, "text" },
            { typeof(decimal).Name, "text" },
            { typeof(double).Name, "real" },
            { typeof(float).Name, "real" },
            { typeof(Guid).Name, "text" },
            { typeof(Int16).Name, "integer" },
            { typeof(Int32).Name, "integer" },
            { typeof(Int64).Name, "integer" },
            { typeof(sbyte).Name, "integer" },
            { typeof(string).Name, "text" },
            { typeof(TimeSpan).Name, "text" },
            { typeof(UInt16).Name, "integer" },
            { typeof(UInt32).Name, "integer" },
            { typeof(UInt64).Name, "integer" }
        };

        SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new GuidHandler());
        SqlMapper.AddTypeHandler(new TimeSpanHandler());
    }

    /// <summary>
    /// Erzeugt SQL-Anweisungen zum Erstellen der Tabelle und ihre Indices.
    /// </summary>
    /// <param name="tableName">Der vorgegebene Name der Tabelle.</param>
    /// <param name="objectType">Der Datentyp, dessen Objekte in der Tabelle gespeichert werden.</param>
    /// <returns>Die erzeugten SQL-Anweisungen.</returns>
    public static List<string> GenerateStatementsToCreateSchema(string tableName, Type objectType)
    {
        var statements = new List<string> { null };
        bool hasAlreadyPrimaryKey = false;
        var columnDefinitions = new StringBuilder();
        PropertyInfo[] allPublicProperties = objectType.GetProperties();
        for (int idx = 0; idx < allPublicProperties.Length; ++idx)
        {
            var propertyInfo = allPublicProperties[idx];
            columnDefinitions.AppendFormat("{0} {1}",
                                         propertyInfo.Name,
                                         clrToSqliteType[propertyInfo.PropertyType.Name]);

            var primaryKeyAttribute = propertyInfo.GetCustomAttribute<RdbTablePrimaryKeyAttribute>();
            if (primaryKeyAttribute != null)
            {
                if (hasAlreadyPrimaryKey)
                {
                    throw new Common.OrmException($"Eine Tabelle für den Typ {objectType.Name} darf nicht in Datenbank erstellt werden: die Klasse verfügt über mehrere Primärschlüsseln!");
                }
                hasAlreadyPrimaryKey = true;

                columnDefinitions.AppendFormat(" not null primary key {0}",
                    primaryKeyAttribute.SortingOrder == ValueSortingOrder.Ascending ? "asc" : "desc");
            }

            if (idx + 1 < allPublicProperties.Length)
            {
                columnDefinitions.Append(", ");
            }

            var tableIndexAttribute = propertyInfo.GetCustomAttribute<RdbTableIndexAttribute>();
            if (tableIndexAttribute != null)
            {
                if (primaryKeyAttribute != null)
                {
                    throw new Common.OrmException($"Eine Tabelle für den Typ {objectType.Name} darf nicht in Datenbank erstellt werden: die Klasse verfügt über eine Eigenschaft, die gleichzeitig als Primärschlüssel und Index gilt!");
                }

                statements.Add($"create index if not exists {tableName}_{propertyInfo.Name} on {tableName} ({propertyInfo.Name} {(tableIndexAttribute.SortingOrder == ValueSortingOrder.Ascending ? "asc" : "desc")}) {tableIndexAttribute.AdditionalSqlClauses};");
            }
        }

        statements[0] = $"create table if not exists {tableName} ({columnDefinitions});";
        return statements;
    }

    /// <summary>
    /// Erzeugt eine SQL-Anweisung zum Ausfüllen einer Tabelle.
    /// </summary>
    /// <param name="tableName">Der vorgegebene Name der Tabelle.</param>
    /// <param name="objectType">Der Datentyp, dessen Objekte in der Tabelle gespeichert werden.</param>
    /// <returns>Eine INSERT-Anweisungen, deren Spalten den Properties des Datentyp <paramref name="objectType"/> entsprechen.</returns>
    public static string GenerateInsertStatement(string tableName, Type objectType)
    {
        PropertyInfo[] allPublicProperties = objectType.GetProperties();
        string[] columnsNames =
            (from propertyInfo in allPublicProperties select propertyInfo.Name).ToArray();

        StringBuilder buffer = new($"insert into {tableName} (");
        for (int idx = 0; idx < columnsNames.Length; ++idx)
        {
            buffer.Append(columnsNames[idx]);
            buffer.Append((idx < columnsNames.Length - 1) ? ',' : ')');
        }

        buffer.Append(" values (");
        for (int idx = 0; idx < columnsNames.Length; ++idx)
        {
            buffer.Append('@');
            buffer.Append(columnsNames[idx]);
            buffer.Append((idx < columnsNames.Length - 1) ? ',' : ')');
        }

        return buffer.ToString();
    }
}
