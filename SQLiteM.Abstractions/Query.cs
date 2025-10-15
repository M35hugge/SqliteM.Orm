using System;

namespace SQLiteM.Abstractions
{
    /// <summary>
    /// Einfache, SQL-freie Query-Beschreibung für Filter (WHERE =)
    /// und Sortierung (ORDER BY) über Spaltennamen.
    /// </summary>
    /// <remarks>
    /// Verwende <see cref="WhereEquals(string, object?)"/> für einen Gleichheits-Filter
    /// und <see cref="OrderBy(string, bool)"/> zum Sortieren.
    /// Wichtiger Hinweis: Es werden die Spaltennamen aus <c>[Column("...")]</c> verwendet,
    /// nicht die C#-Propertynamen.
    /// </remarks>
    public sealed class Query
    {
        /// <summary>
        /// Spaltenname für die WHERE-Bedingung (Gleichheit).
        /// </summary>
        public string? WhereColumn { get; init; }

        /// <summary>
        /// Wert für die WHERE-Bedingung (wird als Parameter gebunden).
        /// </summary>
        public object? WhereValue { get; init; }

        /// <summary>
        /// Spaltenname für ORDER BY.
        /// </summary>
        public string? OrderByColumn { get; init; }

        /// <summary>
        /// Absteigend sortieren, wenn <c>true</c>; sonst aufsteigend.
        /// </summary>
        public bool OrderByDesc { get; init; }

        /// <summary>
        /// Erzeugt eine <see cref="Query"/> mit WHERE-Gleichheitsfilter
        /// auf die angegebene Spalte und den angegebenen Wert.
        /// </summary>
        /// <param name="column">Spaltenname (aus dem <c>[Column]</c>-Attribut).</param>
        /// <param name="value">Vergleichswert (wird parametrisiert).</param>
        /// <returns>Eine neue <see cref="Query"/>-Instanz mit gesetztem WHERE.</returns>
        public static Query WhereEquals(string column, object? value)
            => new() { WhereColumn = column, WhereValue = value };

        /// <summary>
        /// Liefert eine Kopie dieser <see cref="Query"/> mit gesetzter ORDER-BY-Klausel.
        /// </summary>
        /// <param name="column">Spaltenname (aus dem <c>[Column]</c>-Attribut).</param>
        /// <param name="desc">Wenn <c>true</c>, absteigend sortieren.</param>
        /// <returns>Eine neue <see cref="Query"/>-Instanz mit ORDER BY.</returns>
        public Query OrderBy(string column, bool desc = false)
            => new()
            {
                WhereColumn = WhereColumn,
                WhereValue = WhereValue,
                OrderByColumn = column,
                OrderByDesc = desc
            };
    }
}
