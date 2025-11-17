using SQLiteM.Abstractions;
using System.Text;

namespace SQLiteM.Orm.Internal;

/// <summary>
/// Implementierung des <see cref="ISqlBuilder"/>, die SQL-Anweisungen für CRUD-Operationen
/// und Tabellenerstellung basierend auf Entitätsmetadaten generiert.
/// </summary>
/// <remarks>
/// Diese Implementierung verwendet <see cref="IEntityMapper"/>, um Tabellen-, Spalten-
/// und Fremdschlüsseldefinitionen aus den Entitätsattributen zu ermitteln,
/// und <see cref="ISqlDialect"/>, um Identifier und Parameter zu formatieren.
/// Die erzeugten Anweisungen sind mit SQLite kompatibel.
/// </remarks>
/// <seealso cref="ISqlBuilder"/>
/// <seealso cref="IEntityMapper"/>
/// <seealso cref="ISqlDialect"/>
/// <seealso cref="PropertyMap"/>
/// <seealso cref="ForeignKeyMap"/>
public sealed class SqlBuilder : ISqlBuilder
{
    private readonly IEntityMapper _mapper;
    private readonly ISqlDialect _dialect;



    /// <summary>
    /// Initialisiert eine neue Instanz der <see cref="SqlBuilder"/>-Klasse.
    /// </summary>
    /// <param name="mapper">Der Mapper, der Tabellen- und Spaltenmetadaten liefert.</param>
    /// <param name="dialect">Der SQL-Dialekt zum Formatieren von Bezeichnern und Parametern.</param>
    /// <exception cref="ArgumentNullException">Wenn <paramref name="mapper"/> oder <paramref name="dialect"/> <see langword="null"/> ist.</exception>
    public SqlBuilder(IEntityMapper mapper, ISqlDialect dialect)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
    }



    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Wenn <paramref name="entityType"/> <see langword="null"/> ist.</exception>
    /// <exception cref="InvalidOperationException">Wenn für den Entitätstyp keine gemappten Spalten gefunden wurden.</exception>
    public string BuildInsert(Type entityType, out IReadOnlyList<PropertyMap> cols)
    {
        ArgumentNullException.ThrowIfNull(entityType);

        var all = _mapper.GetPropertyMaps(entityType);
        if (all.Count == 0)
            throw new InvalidOperationException($"No mapped columns found for {entityType.Name}.");

        cols = [.. all.Where(c => !c.IsAutoIncrement)];
        var table = _dialect.QuoteIdentifier(_mapper.GetTableName(entityType));
        var colList = string.Join(", ", cols.Select(c => _dialect.QuoteIdentifier(c.ColumnName)));
        var paramList = string.Join(", ", cols.Select(c => _dialect.ParameterPrefix + c.ColumnName));

        return $"INSERT INTO {table} ({colList}) VALUES ({paramList});";
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Wenn <paramref name="entityType"/> <see langword="null"/> ist.</exception>
    /// <exception cref="InvalidOperationException">Wenn keine Spalten gefunden werden oder kein Primärschlüssel definiert ist.</exception>
    public string BuildUpdate(Type entityType, out IReadOnlyList<PropertyMap> cols)
    {
        ArgumentNullException.ThrowIfNull(entityType);

        var all = _mapper.GetPropertyMaps(entityType);
        if (all.Count == 0)
            throw new InvalidOperationException($"No mapped columns found for {entityType.Name}.");

        var key = all.FirstOrDefault(c => c.IsPrimaryKey)
            ?? throw new InvalidOperationException($"PRIMARY KEY is missing for {entityType.Name}.");

        cols = [.. all.Where(c => !c.IsPrimaryKey && !c.IsAutoIncrement)];
        var table = _dialect.QuoteIdentifier(_mapper.GetTableName(entityType));
        var sets = string.Join(", ", cols.Select(c
            => $"{_dialect.QuoteIdentifier(c.ColumnName)} = {_dialect.ParameterPrefix}{c.ColumnName}"));

        return $"UPDATE {table} SET {sets} WHERE {_dialect.QuoteIdentifier(key.ColumnName)} = {_dialect.ParameterPrefix}{key.ColumnName};";
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Wenn <paramref name="entityType"/> <see langword="null"/> ist.</exception>
    /// <exception cref="InvalidOperationException">Wenn kein Primärschlüssel definiert ist.</exception>
    public string BuildDelete(Type entityType, out PropertyMap key)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        key = _mapper.GetPrimaryKey(entityType)
            ?? throw new InvalidOperationException($"PRIMARY KEY is missing for {entityType.Name}.");

        var table = _dialect.QuoteIdentifier(_mapper.GetTableName(entityType));

        return $"DELETE FROM {table} WHERE {_dialect.QuoteIdentifier(key.ColumnName)} = {_dialect.ParameterPrefix}{key.ColumnName};";
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Wenn <paramref name="entityType"/> <see langword="null"/> ist.</exception>
    /// <exception cref="InvalidOperationException">Wenn keine Spalten gefunden werden oder kein Primärschlüssel definiert ist.</exception>
    public string BuildSelectById(Type entityType, out PropertyMap key, out IReadOnlyList<PropertyMap> cols)
    {
        ArgumentNullException.ThrowIfNull(entityType);

        cols = _mapper.GetPropertyMaps(entityType);
        if (cols.Count == 0)
            throw new InvalidOperationException($"No mapped columns found for {entityType.Name}.");

        key = cols.FirstOrDefault(c => c.IsPrimaryKey)
            ?? throw new InvalidOperationException($"PRIMARY KEY is missing for {entityType.Name}.");

        var table = _dialect.QuoteIdentifier(_mapper.GetTableName(entityType));
        var colList = string.Join(", ", cols.Select(c => _dialect.QuoteIdentifier(c.ColumnName)));

        return $"SELECT {colList} FROM {table} WHERE {_dialect.QuoteIdentifier(key.ColumnName)} = {_dialect.ParameterPrefix}{key.ColumnName} LIMIT 1;";
    }


    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Wenn <paramref name="entityType"/> <see langword="null"/> ist.</exception>
    /// <exception cref="InvalidOperationException">Wenn keine Spalten für den Entitätstyp vorhanden sind.</exception>
    /// <remarks>
    /// Spalten, Primärschlüssel, Auto-Increment-Attribute, Spalten-<c>UNIQUE</c> und Fremdschlüssel werden
    /// anhand der im Modell vorhandenen Attribute bestimmt.
    /// Die erzeugte Anweisung nutzt <c>CREATE TABLE IF NOT EXISTS</c>.
    /// </remarks>
    public string BuildCreateTable(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);

        var table = _dialect.QuoteIdentifier(_mapper.GetTableName(entityType));
        var cols = _mapper.GetPropertyMaps(entityType);
        var fks = _mapper.GetForeignKeys(entityType);

        var defs = new List<string>();
        // Spalten
        foreach (var col in cols)
        {
            var typeSql = ToSqlType(col);
            var pk = col.IsPrimaryKey ? " PRIMARY KEY" : string.Empty;
            var ai = col.IsAutoIncrement ? " AUTOINCREMENT" : string.Empty;
            var nn = col.IsNullable || col.IsPrimaryKey ? string.Empty : " NOT NULL";
            var uq = col.IsUniqueColumn ? "UNIQUE" : string.Empty;

            defs.Add($"{_dialect.QuoteIdentifier(col.ColumnName)} {typeSql}{pk}{ai}{nn}{uq}");
        }

        // Fremdschlüssel
        foreach (var fk in fks)
        {
            var onDelete = fk.OnDelete switch
            {
                OnDeleteAction.Restrict => " ON DELETE RESTRICT",
                OnDeleteAction.Cascade => " ON DELETE CASCADE",
                OnDeleteAction.SetNull => " ON DELETE SET NULL",
                OnDeleteAction.SetDefault => " ON DELETE SET DEFAULT",
                _ => string.Empty
            };

            defs.Add(
                $"FOREIGN KEY ({_dialect.QuoteIdentifier(fk.ThisColumn)}) " +
                $"REFERENCES {_dialect.QuoteIdentifier(fk.PrincipalTable)} " +
                $"({_dialect.QuoteIdentifier(fk.PrincipalColumn)}){onDelete}"
            );

            // Korrigiere versehentliche Formatierungsfehler (Leerzeichen, Semikolons)
            for (int i = 0; i < defs.Count; i++)
            {
                defs[i] = defs[i].TrimEnd();
                if (defs[i].EndsWith(';'))
                    defs[i] = defs[i].TrimEnd(';');
            }
        }
        // Zusammenführen (ohne versehentliche Semikolons)
        var sb = new StringBuilder();
        sb.Append($"CREATE TABLE IF NOT EXISTS {table} (");
        sb.Append(string.Join(", ", defs));
        sb.Append(");");
        return sb.ToString();
    }

    /// <summary>
    /// Erzeugt für alle per <see cref="IndexMap"/> gemappten Indizes die entsprechenden
    /// <c>CREATE INDEX</c>- bzw. <c>CREATE UNIQUE INDEX</c>-Anweisungen.
    /// </summary>
    /// <param name="entityType">Der Entitätstyp, dessen Indizes erzeugt werden sollen.</param>
    /// <returns>
    /// Eine schreibgeschützte Liste mit SQL-Befehlen (je Index ein Befehl). Die Liste kann leer sein,
    /// wenn keine Indizes für den Typ definiert sind.
    /// </returns>
    /// <exception cref="ArgumentNullException">Wenn <paramref name="entityType"/> <see langword="null"/> ist.</exception>
    /// <remarks>
    /// Die Indexnamen werden – sofern im Mapping nicht angegeben – nach dem Muster
    /// <c>ix_{tabelle}_{spalte...}</c> generiert. Mehrspaltige Indizes werden in der Reihenfolge
    /// der gemappten Spalten erzeugt.
    /// </remarks>
    public IReadOnlyList<string> BuildCreateIndexes(Type entityType)
    {
        var tableName = _mapper.GetTableName(entityType);
        var table = _dialect.QuoteIdentifier(tableName);
        var indexes = _mapper.GetIndexes(entityType);

        var list = new List<string>();

        foreach (var ix in indexes)
        {
            if (ix.Columns.Count == 0) continue;

            var name = string.IsNullOrWhiteSpace(ix.Name)
                ? $"ix_{tableName}_{string.Join("_", ix.Columns)}"
                : ix.Name!;
            var nameQuoted = _dialect.QuoteIdentifier(name);

            var cols = string.Join(", ", ix.Columns.Select(c => _dialect.QuoteIdentifier(c)));

            string isUnique = ix.IsUnique ? "UNIQUE" : string.Empty;

            var sql = @$"CREATE {isUnique} INDEX IF NOT EXISTS {nameQuoted} ON {table} ({cols});";
            list.Add(sql);
        }
        return list;
    }

    /// <summary>
    /// Übersetzt den CLR-Typ einer Property in den entsprechenden SQLite-Datentyp.
    /// </summary>
    /// <param name="c">Die Property-Metadaten.</param>
    /// <returns>Der SQL-Datentyp als <see cref="string"/>.</returns>
    /// <remarks>
    /// Diese Zuordnung folgt den gängigen Typkonventionen von SQLite:
    /// <list type="bullet">
    /// <item><description><c>int</c>, <c>long</c>, <c>bool</c> → <c>INTEGER</c></description></item>
    /// <item><description><c>double</c>, <c>float</c>, <c>decimal</c> → <c>REAL</c></description></item>
    /// <item><description><c>DateTime</c>, <c>DateTimeOffset</c>, <c>string</c> → <c>TEXT</c></description></item>
    /// </list>
    /// Bei <see cref="string"/> wird – falls <see cref="PropertyMap.Length"/> &gt; 0 – <c>VARCHAR(n)</c> verwendet,
    /// andernfalls <c>TEXT</c>. (SQLite erzwingt Längenangaben nicht.)
    /// </remarks>
    private static string ToSqlType(PropertyMap c)
    {
        var t = c.PropertyType;

        if (t == typeof(int) || t == typeof(long))
            return "INTEGER";

        if (t == typeof(bool))
            return "INTEGER";

        if (t == typeof(double) || t == typeof(float))
            return "REAL";

        if (t == typeof(decimal))
            return "REAL";

        if (t == typeof(DateTime) || t == typeof(DateTimeOffset))
            return "TEXT";

        if (t == typeof(string))
            return c.Length > 0 ? $"VARCHAR({c.Length})" : "TEXT";

        // Fallback: TEXT (SQLite ist dynamisch typisiert)
        return "TEXT";
    }
}
