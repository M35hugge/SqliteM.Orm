using SQLiteM.Abstractions;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;

namespace SQLiteM.Orm.Internal
{
    /// <summary>
    /// Internes, generisches Repository für CRUD-Operationen und einfache Abfragen auf dem Entitätstyp <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// Das Repository arbeitet im Kontext der übergebenen <see cref="IUnitOfWork"/> (Verbindung und Transaktion).
    /// Spalten- und Tabellennamen werden über <see cref="IEntityMapper"/> bestimmt,
    /// SQL wird durch <see cref="ISqlBuilder"/> erzeugt und via <see cref="ISqlDialect"/> (Identifier/Parameter) formatiert.
    /// </remarks>
    /// <typeparam name="T">Der Entitätstyp, der durch dieses Repository verwaltet wird.</typeparam>
    /// <seealso cref="IRepository{T}"/>
    /// <seealso cref="IUnitOfWork"/>
    /// <seealso cref="IEntityMapper"/>
    /// <seealso cref="ISqlBuilder"/>
    /// <seealso cref="ISqlDialect"/>
    internal sealed class Repository<T>(IUnitOfWork uow, IEntityMapper mapper, ISqlBuilder builder, ISqlDialect dialect)
        : IRepository<T> where T : class, new()
    {
        private readonly IUnitOfWork _uow = uow;
        private readonly IEntityMapper _mapper = mapper;
        private readonly ISqlBuilder _sql = builder;
        private readonly ISqlDialect _dialect = dialect;

        /// <summary>
        /// Fügt die angegebene Entität in die Datenbank ein.
        /// </summary>
        /// <param name="entity">Die zu speichernde Entität.</param>
        /// <param name="ct">Ein optionales <see cref="CancellationToken"/>.</param>
        /// <returns>
        /// Die von SQLite vergebene Zeilen-ID (<c>last_insert_rowid()</c>) als <see cref="long"/>,
        /// sofern ein Auto-Increment-Primärschlüssel konfiguriert ist; andernfalls <c>0</c>.
        /// </returns>
        /// <remarks>
        /// Nach erfolgreichem Insert wird – falls vorhanden und beschreibbar – das Primärschlüssel-Property
        /// der Entität mit dem ermittelten Wert aktualisiert.
        /// </remarks>
        public async Task<long> InsertAsync(T entity, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(entity);
            EnsureConnectionAndTransaction();

            var cmdText = _sql.BuildInsert(typeof(T), out var cols);
            using var cmd = _uow.Connection.CreateCommand();
            cmd.Transaction = _uow.Transaction;
            cmd.CommandText = cmdText;

            foreach (var c in cols)
            {
                var prop = typeof(T).GetProperty(c.PropertyName, BindingFlags.Public | BindingFlags.Instance)
                           ?? throw new InvalidOperationException(
                               $"Mapped property '{c.PropertyName}' not found on {typeof(T).Name}. " +
                               "Ensure PropertyMap.PropertyName contains the CLR property name.");

                var value = prop.GetValue(entity);
                AddParameter(cmd, c.ColumnName, value); // DBNull.Value wird intern gesetzt
            }
            await ExecuteNonQueryAsync(cmd, ct).ConfigureAwait(false);

            var key = _mapper.GetPrimaryKey(typeof(T)) 
                ?? throw new InvalidOperationException($"Primary key mapping missing for {typeof(T).Name}.");

            // AutoIncrement → last_insert_rowid(), Id am Objekt setzen und zurückgeben
            if (key is not null && key.IsAutoIncrement)
            {
                cmd.CommandText = "SELECT last_insert_rowid();";

                var idObj = await ExecuteScalarAsync(cmd, ct).ConfigureAwait(false);
                var id64 = Convert.ToInt64(idObj);

                var id = await ExecuteScalarAsync(cmd, ct).ConfigureAwait(false);
                var keyProp = typeof(T).GetProperty(key.PropertyName, BindingFlags.Public | BindingFlags.Instance)
                              ?? throw new InvalidOperationException(
                                  $"Primary key property '{key.PropertyName}' not found on {typeof(T).Name}.");
                if (!keyProp.CanWrite)
                    throw new InvalidOperationException(
                        $"Primary key property '{key.PropertyName}' on {typeof(T).Name} is not writable.");

                keyProp.SetValue(entity, id64);
                return id64;
            }
            // Kein AutoIncrement: best effort – PK-Wert muss gesetzt sein, sonst ist Update/Delete später nicht möglich
            return 0;
        }

        /// <summary>
        /// Aktualisiert die angegebene Entität in der Datenbank.
        /// </summary>
        /// <param name="entity">Die zu aktualisierende Entität.</param>
        /// <param name="ct">Ein optionales <see cref="CancellationToken"/>.</param>
        /// <returns>Die Anzahl der betroffenen Zeilen.</returns>
        /// <remarks>
        /// Es wird vorausgesetzt, dass der Entitätstyp einen Primärschlüssel besitzt und
        /// dessen aktueller Wert in <paramref name="entity"/> gesetzt ist.
        /// </remarks>
        public async Task<int> UpdateAsync(T entity, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(entity);
            EnsureConnectionAndTransaction();

            var cmdText = _sql.BuildUpdate(typeof(T), out var cols);
            using var cmd = _uow.Connection.CreateCommand();
            cmd.Transaction = _uow.Transaction;
            cmd.CommandText = cmdText;

            foreach (var c in cols)
            {
                var prop = typeof(T).GetProperty(c.PropertyName, BindingFlags.Public | BindingFlags.Instance)
                          ?? throw new InvalidOperationException(
                              $"Mapped property '{c.PropertyName}' not found on {typeof(T).Name}.");
                var val = prop.GetValue(entity);
                AddParameter(cmd, c.ColumnName, val);
            }
            var key = _mapper.GetPrimaryKey(typeof(T)) 
                ?? throw new InvalidOperationException($"Primary key mapping missing for {typeof(T).Name}."); 

            var keyVal = typeof(T).GetProperty(key.PropertyName) 
                ?? throw new InvalidOperationException($"Primary key property '{key.PropertyName}' not found on {typeof(T).Name}. Ensure PropertyMap.PropertyName is the CLR property name.");
            if (keyVal is not null && keyVal.GetValue(entity)is not null)
                AddParameter(cmd, key.ColumnName, keyVal.GetValue(entity));

            return await ExecuteNonQueryAsync(cmd, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Löscht einen Datensatz anhand seines Primärschlüsselwertes.
        /// </summary>
        /// <param name="id">Der Primärschlüsselwert der zu löschenden Entität.</param>
        /// <param name="ct">Ein optionales <see cref="CancellationToken"/>.</param>
        /// <returns>Die Anzahl der betroffenen Zeilen.</returns>
        public async Task<int> DeleteAsync(object id, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(id);
            EnsureConnectionAndTransaction();
            var cmdText = _sql.BuildDelete(typeof(T), out var key);
            using var cmd = _uow.Connection.CreateCommand();
            cmd.Transaction = _uow.Transaction;
            cmd.CommandText = cmdText;
            AddParameter(cmd, key.ColumnName, id);
            return await ExecuteNonQueryAsync(cmd, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Sucht eine Entität anhand ihres Primärschlüssels.
        /// </summary>
        /// <param name="id">Der Primärschlüsselwert.</param>
        /// <param name="ct">Ein optionales <see cref="CancellationToken"/>.</param>
        /// <returns>
        /// Die gefundene Entität oder <see langword="null"/>, wenn kein entsprechender Datensatz existiert.
        /// </returns>
    #nullable enable
        public async Task<T?> FindByIdAsync(object id, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(id);
            EnsureConnectionAndTransaction();

            var cmdText = _sql.BuildSelectById(typeof(T), out var key, out var cols);
            using var cmd = _uow.Connection.CreateCommand();
            cmd.Transaction = _uow.Transaction;
            cmd.CommandText = cmdText;

            AddParameter(cmd, key.ColumnName, id);

            using var reader = await ExecuteReaderAsync(cmd, ct).ConfigureAwait(false);
            if (!reader.Read()) return null;

            var entity = new T();
            foreach (var c in cols)
            {
                var prop = typeof(T).GetProperty(c.PropertyName, BindingFlags.Public | BindingFlags.Instance);
                if (prop is null) continue;

                var ordinal = reader.GetOrdinal(c.ColumnName);
                if (reader.IsDBNull(ordinal))
                {
                    prop.SetValue(entity, null);
                    continue;
                }
                object value = reader.GetValue(ordinal);
                prop.SetValue(entity, ConvertTo(prop.PropertyType, value));
            }
            return entity;
        }

        /// <summary>
        /// Lädt alle Datensätze der zugrunde liegenden Tabelle.
        /// </summary>
        /// <param name="ct">Ein optionales <see cref="CancellationToken"/>.</param>
        /// <returns>Eine schreibgeschützte Liste aller Entitäten.</returns>
        public async Task<IReadOnlyList<T>> FindAllAsync(CancellationToken ct = default)
        {
            EnsureConnectionAndTransaction();
            var cols = _mapper.GetPropertyMaps(typeof(T));
            if (cols.Count == 0)
                throw new InvalidOperationException($"No mapped columns found for {typeof(T).Name}.");

            var table = _dialect.QuoteIdentifier(_mapper.GetTableName(typeof(T)));
            var colList = string.Join(", ", cols.Select(c => _dialect.QuoteIdentifier(c.ColumnName)));
            var sql = $"SELECT {colList} FROM {table};";

            using var cmd = _uow.Connection.CreateCommand();
            cmd.Transaction = _uow.Transaction;
            cmd.CommandText = sql;

            using var reader = await ExecuteReaderAsync(cmd, ct).ConfigureAwait(false);
            return MaterializeList(reader, cols);
        }

        /// <summary>
        /// Führt eine einfache, parametrisierte Abfrage gegen die Tabelle aus.
        /// </summary>
        /// <param name="query">Die Abfragebeschreibung (WHERE/ORDER BY).</param>
        /// <param name="ct">Ein optionales <see cref="CancellationToken"/>.</param>
        /// <returns>Eine schreibgeschützte Liste der gefundenen Entitäten.</returns>
        /// <remarks>
        /// Unterstützt eine Gleichheitsbedingung (<c>WHERE &lt;Spalte&gt; = @param</c>) sowie
        /// <c>ORDER BY</c> mit optionaler absteigender Sortierung.
        /// </remarks>
        /// <seealso cref="Query"/>
        public async Task<IReadOnlyList<T>> QueryAsync(Query query, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(query);
            EnsureConnectionAndTransaction();

            var cols = _mapper.GetPropertyMaps(typeof(T));
            if (cols.Count == 0)
                throw new InvalidOperationException($"No mapped columns found for {typeof(T).Name}.");

            var table = _dialect.QuoteIdentifier(_mapper.GetTableName(typeof(T)));
            var colList = string.Join(", ", cols.Select(c => _dialect.QuoteIdentifier(c.ColumnName)));

            // Validierung der Spaltennamen (Where/OrderBy beziehen sich auf DB-Spaltennamen)
            if (!string.IsNullOrWhiteSpace(query.WhereColumn) &&
                !cols.Any(c => string.Equals(c.ColumnName, query.WhereColumn, StringComparison.Ordinal)))
            {
                throw new ArgumentException(
                    $"Unknown column '{query.WhereColumn}' for entity {typeof(T).Name}. " +
                    "Use the mapped database column name (not the CLR property name).", nameof(query));
            }
            if (!string.IsNullOrWhiteSpace(query.OrderByColumn) &&
                !cols.Any(c => string.Equals(c.ColumnName, query.OrderByColumn, StringComparison.Ordinal)))
            {
                throw new ArgumentException(
                    $"Unknown column '{query.OrderByColumn}' for entity {typeof(T).Name}. " +
                    "Use the mapped database column name (not the CLR property name).", nameof(query));
            }

            var sb = new StringBuilder($"SELECT {colList} FROM {table}");
            using var cmd = _uow.Connection.CreateCommand();
            cmd.Transaction = _uow.Transaction;

            if (!string.IsNullOrWhiteSpace(query.WhereColumn))
            {
                sb.Append(" WHERE ");
                sb.Append(_dialect.QuoteIdentifier(query.WhereColumn!));
                sb.Append(" = ");
                sb.Append(_dialect.ParameterPrefix);
                sb.Append(query.WhereColumn);
                AddParameter(cmd, query.WhereColumn!, query.WhereValue);
            }

            if (!string.IsNullOrWhiteSpace(query.OrderByColumn))
            {
                sb.Append(" ORDER BY ");
                sb.Append(_dialect.QuoteIdentifier(query.OrderByColumn!));
                sb.Append(query.OrderByDesc ? " DESC" : " ASC");
            }

            sb.Append(';');
            cmd.CommandText = sb.ToString();

            using var reader = await ExecuteReaderAsync(cmd, ct).ConfigureAwait(false);
            return MaterializeList(reader, cols);
        }

        // Interne Hilfsmethoden (ohne öffentliche API-Oberfläche)

        /// <summary>
        /// Materialisiert eine Liste von Entitäten aus einem Datenleser anhand der Mapping-Informationen.
        /// </summary>
        private static List<T> MaterializeList(IDataReader reader, IReadOnlyList<PropertyMap> cols)
        {
            var list = new List<T>();
            while (reader.Read())
            {
                var e = new T();
                foreach (var c in cols)
                {
                    var prop = typeof(T).GetProperty(c.PropertyName, BindingFlags.Public | BindingFlags.Instance);
                    if (prop is null) continue;

                    var ordinal = reader.GetOrdinal(c.ColumnName);
                    if (reader.IsDBNull(ordinal)) { prop.SetValue(e, null); continue; }

                    var v = reader.GetValue(ordinal);
                    prop.SetValue(e, ConvertTo(prop.PropertyType, v));
                }
                list.Add(e);
            }
            return list;
        }

        /// <summary>
        /// Fügt einen parameterisierten Wert hinzu. Null wird automatisch zu <see cref="DBNull.Value"/>.
        /// </summary>
        private void AddParameter(IDbCommand cmd, string name, object? value)
        {
            ArgumentNullException.ThrowIfNull(cmd);
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Parameter name must not be empty.", nameof(name));

            var p = cmd.CreateParameter();
            p.ParameterName = _dialect.ParameterPrefix + name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        /// <summary>
        /// Stellt sicher, dass <see cref="IUnitOfWork.Connection"/> und <see cref="IUnitOfWork.Transaction"/> gesetzt sind.
        /// </summary>
        private void EnsureConnectionAndTransaction()
        {
            if (_uow.Connection is null)
                throw new InvalidOperationException("UnitOfWork.Connection is null. Ensure the UnitOfWork is properly created and not disposed.");
            if (_uow.Transaction is null)
                throw new InvalidOperationException("UnitOfWork.Transaction is null. Ensure the UnitOfWork has not been committed or disposed.");
        }

        /// <summary>
        /// Führt <see cref="IDbCommand.ExecuteNonQuery"/> asynchron aus (mit Fallback auf synchron).
        /// </summary>
        private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(command);
            return command switch
            {
                DbCommand dbCmd => await dbCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false),
                _ => command.ExecuteNonQuery()
            };
        }

        /// <summary>
        /// Führt <see cref="IDbCommand.ExecuteScalar"/> asynchron aus (mit Fallback auf synchron).
        /// </summary>
        private static async Task<object> ExecuteScalarAsync(IDbCommand command, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(command);
            return command switch
            {
                DbCommand dbCmd => await dbCmd.ExecuteScalarAsync(ct).ConfigureAwait(false) ?? 0,
                _ => command.ExecuteScalar() ?? 0
            };
        }

        /// <summary>
        /// Führt <see cref="IDbCommand.ExecuteReader()"/> asynchron aus (mit Fallback auf synchron).
        /// </summary>
        private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(command);
            return command switch
            {
                DbCommand dbCmd => await dbCmd.ExecuteReaderAsync(ct).ConfigureAwait(false),
                _ => command.ExecuteReader()
            };
        }

        /// <summary>
        /// Einfache Typkonvertierung von SQLite-Werten auf CLR-Zieltypen, inkl. Nullable-Unterstützung.
        /// </summary>
        private static object? ConvertTo(Type targetType, object value)
        {
            if (targetType == typeof(string)) return value.ToString();
            if (targetType == typeof(int)) return Convert.ToInt32(value);
            if (targetType == typeof(long)) return Convert.ToInt64(value);
            if (targetType == typeof(bool)) return Convert.ToInt64(value) != 0;
            if (targetType == typeof(double)) return Convert.ToDouble(value);
            if (targetType == typeof(float)) return Convert.ToSingle(value);
            if (targetType == typeof(decimal)) return Convert.ToDecimal(value);
            if (targetType == typeof(DateTime)) return DateTime.Parse(value.ToString()!);

            if (Nullable.GetUnderlyingType(targetType) is Type u)
                return ConvertTo(u, value);

            return value;
        }
    }
}
