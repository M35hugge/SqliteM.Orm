using SQLiteM.Abstractions;
using System.Text;
using System.Linq;

namespace SQLiteM.Orm.Internal
{
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
    public sealed class SqlBuilder(IEntityMapper mapper, ISqlDialect dialect) : ISqlBuilder
    {
        private readonly IEntityMapper _mapper = mapper;
        private readonly ISqlDialect _dialect = dialect;

        /// <summary>
        /// Erstellt eine SQL-<c>INSERT</c>-Anweisung für den angegebenen Entitätstyp.
        /// </summary>
        /// <param name="entityType">Der Entitätstyp, für den die Anweisung erzeugt werden soll.</param>
        /// <param name="cols">Gibt die Liste der Spalten zurück, die in das <c>INSERT</c> einbezogen werden.</param>
        /// <returns>Die generierte SQL-<c>INSERT</c>-Anweisung.</returns>
        /// <remarks>
        /// Spalten, die als <see cref="PropertyMap.IsAutoIncrement"/> markiert sind, werden automatisch ausgeschlossen.
        /// </remarks>
        public string BuildInsert(Type entityType, out IReadOnlyList<PropertyMap> cols)
        {
            var all = _mapper.GetPropertyMaps(entityType);

            cols = [.. all.Where(c => !c.IsAutoIncrement)];
            var table = _dialect.QuoteIdentifier(_mapper.GetTableName(entityType));
            var colList = string.Join(", ", cols.Select(c => _dialect.QuoteIdentifier(c.ColumnName)));
            var paramList = string.Join(", ", cols.Select(c => _dialect.ParameterPrefix + c.ColumnName));

            return $"INSERT INTO {table} ({colList}) VALUES ({paramList});";
        }

        /// <summary>
        /// Erstellt eine SQL-<c>UPDATE</c>-Anweisung für den angegebenen Entitätstyp.
        /// </summary>
        /// <param name="entityType">Der Entitätstyp, für den die Anweisung erzeugt werden soll.</param>
        /// <param name="cols">Gibt die Liste der Spalten zurück, die im <c>SET</c>-Teil der Anweisung enthalten sind.</param>
        /// <returns>Die generierte SQL-<c>UPDATE</c>-Anweisung.</returns>
        /// <exception cref="InvalidOperationException">Wenn kein Primärschlüssel definiert ist.</exception>
        /// <remarks>
        /// Auto-Increment- und Primärschlüsselspalten werden im Update ausgeschlossen.
        /// </remarks>
        public string BuildUpdate(Type entityType, out IReadOnlyList<PropertyMap> cols)
        {
            var all = _mapper.GetPropertyMaps(entityType);
            var key = all.FirstOrDefault(c => c.IsPrimaryKey)
                ?? throw new InvalidOperationException("PRIMARY KEY is missing");

            cols = [.. all.Where(c => !c.IsPrimaryKey && !c.IsAutoIncrement)];
            var table = _dialect.QuoteIdentifier(_mapper.GetTableName(entityType));
            var sets = string.Join(", ", cols.Select(c
                => $"{_dialect.QuoteIdentifier(c.ColumnName)} = {_dialect.ParameterPrefix}{c.ColumnName}"));

            return $"UPDATE {table} SET {sets} WHERE {_dialect.QuoteIdentifier(key.ColumnName)} = {_dialect.ParameterPrefix}{key.ColumnName};";
        }

        /// <summary>
        /// Erstellt eine SQL-<c>DELETE</c>-Anweisung für den angegebenen Entitätstyp.
        /// </summary>
        /// <param name="entityType">Der Entitätstyp, für den die Anweisung erzeugt werden soll.</param>
        /// <param name="key">Gibt die Primärschlüsselspalte der Tabelle zurück.</param>
        /// <returns>Die generierte SQL-<c>DELETE</c>-Anweisung.</returns>
        /// <exception cref="InvalidOperationException">Wenn kein Primärschlüssel definiert ist.</exception>
        public string BuildDelete(Type entityType, out PropertyMap key)
        {
            key = _mapper.GetPrimaryKey(entityType)
                ?? throw new InvalidOperationException("PRIMARY KEY is missing");

            var table = _dialect.QuoteIdentifier(_mapper.GetTableName(entityType));

            return $"DELETE FROM {table} WHERE {_dialect.QuoteIdentifier(key.ColumnName)} = {_dialect.ParameterPrefix}{key.ColumnName};";
        }

        /// <summary>
        /// Erstellt eine SQL-<c>SELECT</c>-Anweisung zur Abfrage einer Entität anhand ihres Primärschlüssels.
        /// </summary>
        /// <param name="entityType">Der Entitätstyp, für den die Anweisung erzeugt werden soll.</param>
        /// <param name="key">Gibt die Primärschlüsselspalte der Tabelle zurück.</param>
        /// <param name="cols">Gibt die Spaltenliste der Tabelle zurück.</param>
        /// <returns>Die generierte SQL-<c>SELECT</c>-Anweisung.</returns>
        /// <exception cref="InvalidOperationException">Wenn kein Primärschlüssel definiert ist.</exception>
        public string BuildSelectById(Type entityType, out PropertyMap key, out IReadOnlyList<PropertyMap> cols)
        {
            cols = _mapper.GetPropertyMaps(entityType);
            key = cols.FirstOrDefault(c => c.IsPrimaryKey)
                ?? throw new InvalidOperationException("PRIMARY KEY is missing");

            var table = _dialect.QuoteIdentifier(_mapper.GetTableName(entityType));
            var colList = string.Join(", ", cols.Select(c => _dialect.QuoteIdentifier(c.ColumnName)));

            return $"SELECT {colList} FROM {table} WHERE {_dialect.QuoteIdentifier(key.ColumnName)} = {_dialect.ParameterPrefix}{key.ColumnName} LIMIT 1;";
        }

        /// <summary>
        /// Erstellt eine SQL-<c>CREATE TABLE</c>-Anweisung für den angegebenen Entitätstyp.
        /// </summary>
        /// <param name="entityType">Der Entitätstyp, dessen Tabellendefinition erzeugt werden soll.</param>
        /// <returns>Die generierte SQL-<c>CREATE TABLE</c>-Anweisung.</returns>
        /// <remarks>
        /// Spalten, Primärschlüssel, Auto-Increment-Attribute und Fremdschlüssel werden
        /// anhand der im Modell vorhandenen Attribute bestimmt.
        /// Die erzeugte Anweisung nutzt standardmäßig <c>CREATE TABLE IF NOT EXISTS</c>.
        /// </remarks>
        public string BuildCreateTable(Type entityType)
        {
            var table = _dialect.QuoteIdentifier(_mapper.GetTableName(entityType));
            var cols = _mapper.GetPropertyMaps(entityType);
            var fks = _mapper.GetForeignKeys(entityType);

            var sb = new StringBuilder();
            sb.Append($"CREATE TABLE IF NOT EXISTS {table} (");

            var defs = new List<string>();
            foreach (var col in cols)
            {
                var typeSql = ToSqlType(col);
                var pk = col.IsPrimaryKey ? " PRIMARY KEY" : string.Empty;
                var ai = col.IsAutoIncrement ? " AUTOINCREMENT" : string.Empty;
                var nn = col.IsNullable || col.IsPrimaryKey ? string.Empty : " NOT NULL";

                defs.Add($"{_dialect.QuoteIdentifier(col.ColumnName)} {typeSql}{pk}{ai}{nn}");
            }

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

            sb.Append(string.Join(", ", defs));
            sb.Append(");");
            return sb.ToString();
        }

        /// <summary>
        /// Übersetzt den .NET-Datentyp eines Propertys in den entsprechenden SQLite-Datentyp.
        /// </summary>
        /// <param name="c">Die Property-Metadaten.</param>
        /// <returns>Der SQL-Datentyp als String.</returns>
        /// <remarks>
        /// Diese Zuordnung folgt den gängigen Typkonventionen von SQLite:
        /// <list type="bullet">
        /// <item><description><c>int</c>, <c>long</c>, <c>bool</c> → <c>INTEGER</c></description></item>
        /// <item><description><c>double</c>, <c>float</c>, <c>decimal</c> → <c>REAL</c></description></item>
        /// <item><description><c>DateTime</c>, <c>DateTimeOffset</c>, <c>string</c> → <c>TEXT</c></description></item>
        /// </list>
        /// Bei Zeichenfolgen berücksichtigt die Methode den optionalen Längenwert
        /// (<see cref="PropertyMap.Length"/>), sofern dieser größer als null ist.
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

            return "TEXT";
        }
    }
}
