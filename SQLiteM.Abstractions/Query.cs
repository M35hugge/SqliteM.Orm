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
        // ===== Neue API (preferred) =====

        /// <summary>Interne Liste der Bedingungen (AND-verknüpft).</summary>
        public List<Condition> Conditions { get; } = new();

        /// <summary>Spalte für ORDER BY (DB- oder CLR-Name; wird beim Ausführen aufgelöst).</summary>
        public string? OrderByColumn { get; private set; }

        /// <summary>Absteigende Sortierung?</summary>
        public bool OrderByDesc { get; private set; }

        // Fluent-Erzeuger
        public static Query WhereEquals(string column, object? value) => new Query().AndEquals(column, value);
        public static Query WhereGreater(string column, object value) => new Query().AndGreater(column, value);
        public static Query WhereGreaterOrEquals(string column, object value) => new Query().AndGreaterOrEquals(column, value);
        public static Query WhereLess(string column, object value) => new Query().AndLess(column, value);
        public static Query WhereLessOrEquals(string column, object value) => new Query().AndLessOrEquals(column, value);

        // Fluent-AND
        public Query AndEquals(string column, object? value) => Add(column, Operator.Eq, value);
        public Query AndGreater(string column, object value) => Add(column, Operator.Gt, value);
        public Query AndGreaterOrEquals(string column, object value) => Add(column, Operator.Ge, value);
        public Query AndLess(string column, object value) => Add(column, Operator.Lt, value);
        public Query AndLessOrEquals(string column, object value) => Add(column, Operator.Le, value);

        public Query OrderBy(string column, bool desc = false)
        {
            OrderByColumn = column;
            OrderByDesc = desc;
            return this;
        }

        private Query Add(string column, Operator op, object? value)
        {
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Column must not be empty.", nameof(column));

            // Für >, >=, <, <= darf value nicht null sein (SQL vergleicht NULL nicht)
            if ((op == Operator.Gt || op == Operator.Ge || op == Operator.Lt || op == Operator.Le) && value is null)
                throw new ArgumentException("Comparison operators do not accept null. Use AndEquals(column, null) for IS NULL.", nameof(value));

            Conditions.Add(new Condition(column, op, value));
            return this;
        }

        // ===== Alte API (deprecated, bleibt funktionsfähig) =====

        /// <summary>DEPRECATED: Nur für Abwärtskompatibilität.</summary>
        public string? WhereColumn { get; set; }

        /// <summary>DEPRECATED: Nur für Abwärtskompatibilität.</summary>
        public object? WhereValue { get; set; }

        /// <summary>
        /// Interne Hilfe zum „Hochheben“ der alten Felder in Conditions
        /// </summary>
        public void NormalizeLegacy()
        {
            if (WhereColumn is not null)
            {
                // Nur dann übernehmen, wenn noch keine neue Condition existiert
                if (Conditions.Count == 0)
                    Conditions.Add(new Condition(WhereColumn, Operator.Eq, WhereValue));

                // Danach ignorieren wir die alten Felder (keine doppelte Anwendung)
            }
        }
    }

    /// <summary>Eine WHERE-Bedingung (Spalte, Operator, Wert).</summary>
    public readonly record struct Condition(string Column, Operator Op, object? Value);

    /// <summary>Unterstützte Vergleichsoperatoren.</summary>
    public enum Operator { Eq, Gt, Ge, Lt, Le }
}
