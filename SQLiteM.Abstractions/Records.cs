using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteM.Abstractions
{
    // -------------------------
    // Helper-Records (Mapping)
    // -------------------------
    [CompilerGenerated]
    public static class Records { }

    /// <summary>
    /// Beschreibt die Zuordnung einer CLR-Property zu einer DB-Spalte.
    /// </summary>
    /// <param name="ColumnName">Spaltenname in der Tabelle.</param>
    /// <param name="PropertyName">Name der CLR-Property.</param>
    /// <param name="PropertyType">CLR-Typ der Property.</param>
    /// <param name="IsPrimaryKey">Ob die Spalte Teil des Primärschlüssels ist.</param>
    /// <param name="IsAutoIncrement">Ob der Wert automatisch erhöht wird.</param>
    /// <param name="IsNullable">Ob NULL-Werte erlaubt sind.</param>
    /// <param name="Length">Maximale Länge (nur relevant für <c>string</c>).</param>
    public sealed record PropertyMap(
        string ColumnName,
        string PropertyName,
        Type PropertyType,
        bool IsPrimaryKey,
        bool IsAutoIncrement,
        bool IsNullable,
        int Length
    );

    /// <summary>
    /// Beschreibt eine Fremdschlüsselbeziehung.
    /// </summary>
    /// <param name="ThisColumn">Lokale Spalte (FK) in der aktuellen Tabelle.</param>
    /// <param name="PrincipalEntity">Typ der referenzierten Entität.</param>
    /// <param name="PrincipalTable">Tabellenname der referenzierten Entität.</param>
    /// <param name="PrincipalColumn">Spaltenname des referenzierten Primärschlüssels bzw. einer UNIQUE-Spalte.</param>
    /// <param name="OnDelete">Aktion bei Löschung der referenzierten Zeile.</param>
    public sealed record ForeignKeyMap(
        string ThisColumn,
        Type PrincipalEntity,
        string PrincipalTable,
        string PrincipalColumn,
        OnDeleteAction OnDelete
    );
}
