namespace SQLiteM.Abstractions;

/// <summary>
/// Einfache, SQL-freie Query-Beschreibung für Filter (WHERE) und Sortierung (ORDER BY).
/// </summary>
/// <remarks>
/// Spalten können als DB-Spaltenname <em>oder</em> als CLR-Propertyname angegeben werden.
/// Die tatsächliche Auflösung auf DB-Spaltennamen erfolgt im Repository.
/// Für Gleichheit mit <c>null</c> wird automatisch <c>IS NULL</c> erzeugt.
/// Vergleichsoperatoren (&gt;, &gt;=, &lt;, &lt;=) akzeptieren kein <c>null</c>.
/// </remarks>
public sealed class Query
{
    /// <summary>
    /// Interne Liste der WHERE-Bedingungen (AND-verknüpft, in Eingabereihenfolge).
    /// </summary>
    public List<Condition> Conditions { get; } = new();

    /// <summary>
    /// Spalte für ORDER BY (DB- oder CLR-Name; wird beim Ausführen aufgelöst).
    /// </summary>
    public string? OrderByColumn { get; private set; }

    /// <summary>
    /// <see langword="true"/>, wenn die Sortierung absteigend erfolgen soll.
    /// </summary>
    public bool OrderByDesc { get; private set; }

    // ---------- Fluent-Erzeuger (Convenience) ----------

    /// <summary>
    /// Erstellt eine neue Abfrage mit einer Gleichheitsbedingung.
    /// </summary>
    /// <param name="column">Spalten- oder CLR-Propertyname.</param>
    /// <param name="value">Vergleichswert; bei <c>null</c> wird <c>IS NULL</c> erzeugt.</param>
    public static Query WhereEquals(string column, object? value) => new Query().AndEquals(column, value);

    /// <summary>
    /// Erstellt eine neue Abfrage mit einer &quot;größer als&quot;-Bedingung (<c>&gt;</c>).
    /// </summary>
    /// <param name="column">Spalten- oder CLR-Propertyname.</param>
    /// <param name="value">Vergleichswert (darf nicht <c>null</c> sein).</param>
    public static Query WhereGreater(string column, object value) => new Query().AndGreater(column, value);

    /// <summary>
    /// Erstellt eine neue Abfrage mit einer &quot;größer gleich&quot;-Bedingung (<c>&gt;=</c>).
    /// </summary>
    /// <param name="column">Spalten- oder CLR-Propertyname.</param>
    /// <param name="value">Vergleichswert (darf nicht <c>null</c> sein).</param>
    public static Query WhereGreaterOrEquals(string column, object value) => new Query().AndGreaterOrEquals(column, value);

    /// <summary>
    /// Erstellt eine neue Abfrage mit einer &quot;kleiner als&quot;-Bedingung (<c>&lt;</c>).
    /// </summary>
    /// <param name="column">Spalten- oder CLR-Propertyname.</param>
    /// <param name="value">Vergleichswert (darf nicht <c>null</c> sein).</param>
    public static Query WhereLess(string column, object value) => new Query().AndLess(column, value);

    /// <summary>
    /// Erstellt eine neue Abfrage mit einer &quot;kleiner gleich&quot;-Bedingung (<c>&lt;=</c>).
    /// </summary>
    /// <param name="column">Spalten- oder CLR-Propertyname.</param>
    /// <param name="value">Vergleichswert (darf nicht <c>null</c> sein).</param>
    public static Query WhereLessOrEquals(string column, object value) => new Query().AndLessOrEquals(column, value);

    // ---------- Fluent-AND (verkettbar) ----------

    /// <summary>
    /// Fügt eine Gleichheitsbedingung (<c>=</c>) hinzu. Bei <paramref name="value"/> = <c>null</c> wird <c>IS NULL</c> erzeugt.
    /// </summary>
    /// <param name="column">Spalten- oder CLR-Propertyname.</param>
    /// <param name="value">Vergleichswert; <c>null</c> erzeugt <c>IS NULL</c>.</param>
    /// <returns>Dieselbe <see cref="Query"/>-Instanz (verkettbar).</returns>
    public Query AndEquals(string column, object? value) => Add(column, Operator.Eq, value);

    /// <summary>
    /// Fügt eine &quot;größer als&quot;-Bedingung (<c>&gt;</c>) hinzu.
    /// </summary>
    /// <param name="column">Spalten- oder CLR-Propertyname.</param>
    /// <param name="value">Vergleichswert (darf nicht <c>null</c> sein).</param>
    /// <returns>Dieselbe <see cref="Query"/>-Instanz (verkettbar).</returns>
    public Query AndGreater(string column, object value) => Add(column, Operator.Gt, value);

    /// <summary>
    /// Fügt eine &quot;größer gleich&quot;-Bedingung (<c>&gt;=</c>) hinzu.
    /// </summary>
    /// <param name="column">Spalten- oder CLR-Propertyname.</param>
    /// <param name="value">Vergleichswert (darf nicht <c>null</c> sein).</param>
    /// <returns>Dieselbe <see cref="Query"/>-Instanz (verkettbar).</returns>
    public Query AndGreaterOrEquals(string column, object value) => Add(column, Operator.Ge, value);

    /// <summary>
    /// Fügt eine &quot;kleiner als&quot;-Bedingung (<c>&lt;</c>) hinzu.
    /// </summary>
    /// <param name="column">Spalten- oder CLR-Propertyname.</param>
    /// <param name="value">Vergleichswert (darf nicht <c>null</c> sein).</param>
    /// <returns>Dieselbe <see cref="Query"/>-Instanz (verkettbar).</returns>
    public Query AndLess(string column, object value) => Add(column, Operator.Lt, value);

    /// <summary>
    /// Fügt eine &quot;kleiner gleich&quot;-Bedingung (<c>&lt;=</c>) hinzu.
    /// </summary>
    /// <param name="column">Spalten- oder CLR-Propertyname.</param>
    /// <param name="value">Vergleichswert (darf nicht <c>null</c> sein).</param>
    /// <returns>Dieselbe <see cref="Query"/>-Instanz (verkettbar).</returns>
    public Query AndLessOrEquals(string column, object value) => Add(column, Operator.Le, value);

    /// <summary>
    /// Setzt ORDER BY auf die angegebene Spalte.
    /// </summary>
    /// <param name="column">Spalten- oder CLR-Propertyname.</param>
    /// <param name="desc"><see langword="true"/>, um absteigend zu sortieren; sonst aufsteigend.</param>
    /// <returns>Dieselbe <see cref="Query"/>-Instanz (verkettbar).</returns>
    public Query OrderBy(string column, bool desc = false)
    {
        OrderByColumn = column;
        OrderByDesc = desc;
        return this;
    }

    /// <summary>
    /// Übernimmt (falls gesetzt) die Legacy-Felder <see cref="WhereColumn"/>/<see cref="WhereValue"/> als erste Bedingung.
    /// </summary>
    public void NormalizeLegacy()
    {
        if (WhereColumn is not null && Conditions.Count == 0)
            Conditions.Add(new Condition(WhereColumn, Operator.Eq, WhereValue));
    }

    // ---------- intern ----------

    private Query Add(string column, Operator op, object? value)
    {
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Column must not be empty.", nameof(column));

        // Für >, >=, <, <= ist null semantisch nicht zulässig (SQL: UNKNOWN).
        if ((op == Operator.Gt || op == Operator.Ge || op == Operator.Lt || op == Operator.Le) && value is null)
            throw new ArgumentException("Comparison operators do not accept null. Use AndEquals(column, null) for IS NULL.", nameof(value));

        Conditions.Add(new Condition(column, op, value));
        return this;
    }

    // ===== Alte API (deprecated) – bleibt aus Kompatibilitätsgründen vorhanden =====

    /// <summary>DEPRECATED: Einzelne WHERE-Spalte (wird beim Ausführen in <see cref="Conditions"/> überführt).</summary>
    public string? WhereColumn { get; set; }

    /// <summary>DEPRECATED: Einzelner WHERE-Wert (wird beim Ausführen in <see cref="Conditions"/> überführt).</summary>
    public object? WhereValue { get; set; }
}

/// <summary>
/// Eine WHERE-Bedingung (Spalte, Operator, Wert).
/// </summary>
/// <param name="Column">Spalten- oder CLR-Propertyname (wird später auf die DB-Spalte gemappt).</param>
/// <param name="Op">Vergleichsoperator.</param>
/// <param name="Value">Vergleichswert; bei <see cref="Operator.Eq"/> darf er <c>null</c> sein (→ <c>IS NULL</c>).</param>
public readonly record struct Condition(string Column, Operator Op, object? Value);

/// <summary>
/// Unterstützte Vergleichsoperatoren.
/// </summary>
public enum Operator
{
    /// <summary>Gleich (<c>=</c>). Bei Wert <c>null</c> wird <c>IS NULL</c> erzeugt.</summary>
    Eq,
    /// <summary>Größer als (<c>&gt;</c>). Wert darf nicht <c>null</c> sein.</summary>
    Gt,
    /// <summary>Größer gleich (<c>&gt;=</c>). Wert darf nicht <c>null</c> sein.</summary>
    Ge,
    /// <summary>Kleiner als (<c>&lt;</c>). Wert darf nicht <c>null</c> sein.</summary>
    Lt,
    /// <summary>Kleiner gleich (<c>&lt;=</c>). Wert darf nicht <c>null</c> sein.</summary>
    Le
}
