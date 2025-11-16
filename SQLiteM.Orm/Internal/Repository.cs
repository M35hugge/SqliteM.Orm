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
#nullable enable
    internal sealed class Repository<T>(IUnitOfWork uow, IEntityMapper mapper, ISqlBuilder builder, ISqlDialect dialect, INameTranslator? translator)
        : IRepository<T> where T : class, new()
    {

        private readonly INameTranslator _names = translator ?? throw new ArgumentNullException(nameof(translator));
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
        /// Die von SQLite vergebene Zeilen-ID (<c>last_insert_rowid()</c>) als <see cref="int"/>,
        /// sofern ein Auto-Increment-Primärschlüssel konfiguriert ist; andernfalls <c>0</c>.
        /// </returns>
        /// <remarks>
        /// Nach erfolgreichem Insert wird – falls vorhanden und beschreibbar – das Primärschlüssel-Property
        /// der Entität mit dem ermittelten Wert aktualisiert.
        /// </remarks>
        public async Task<int> InsertAsync(T entity, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(entity);
            EnsureConnectionAndTransaction();

            var cmdText = _sql.BuildInsert(typeof(T), out var cols);
            using var cmd = _uow.Connection.CreateCommand();
            cmd.Transaction = _uow.Transaction;
            cmd.CommandText = cmdText;

            foreach (var c in cols)
            {
                var prop = typeof(T).GetProperty(_names.Property(c.PropertyName), BindingFlags.Public | BindingFlags.Instance)
                           ?? throw new InvalidOperationException(
                               $"Mapped property '{c.PropertyName}' not found on {typeof(T).Name}. " +
                               "Ensure PropertyMap.PropertyName contains the CLR property name.");

                var value = prop.GetValue(entity);
                AddParameter(cmd, c.ColumnName, value); // DBNull.Value wird intern gesetzt
            }
            await ExecuteNonQueryAsync(cmd, ct).ConfigureAwait(false);

            //var key = _mapper.GetPrimaryKey(typeof(T))
            //    ?? throw new InvalidOperationException($"Primary key mapping missing for {typeof(T).Name}.");
            var key = _mapper.GetPrimaryKey(typeof(T));
            // AutoIncrement → last_insert_rowid(), Id am Objekt setzen und zurückgeben
            if (key is not null && key.IsAutoIncrement)
            {
                cmd.CommandText = "SELECT last_insert_rowid();";

                var idObj = await ExecuteScalarAsync(cmd, ct).ConfigureAwait(false);
                var id32 = Convert.ToInt32(idObj);

                var id = await ExecuteScalarAsync(cmd, ct).ConfigureAwait(false);
                var keyProp = typeof(T).GetProperty(key.PropertyName, BindingFlags.Public | BindingFlags.Instance)
                              ?? throw new InvalidOperationException(
                                  $"Primary key property '{key.PropertyName}' not found on {typeof(T).Name}.");
                if (!keyProp.CanWrite)
                    throw new InvalidOperationException(
                        $"Primary key property '{key.PropertyName}' on {typeof(T).Name} is not writable.");

                keyProp.SetValue(entity, id32);
                return id32;
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
            if (keyVal is not null && keyVal.GetValue(entity) is not null)
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
        /// <exception cref="InvalidOperationException">Wenn keine Spalten gemappt sind.</exception>
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
        /// <para>
        /// Für <paramref name="query"/> können sowohl CLR-Propertynamen als auch DB-Spaltennamen verwendet werden;
        /// die Auflösung erfolgt über <see cref="ResolveColumnName(string)"/>.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Wenn keine Spalten gemappt sind.</exception>
        /// <exception cref="ArgumentException">Wenn unbekannte Spalten-/Propertynamen verwendet werden.</exception>
        public async Task<IReadOnlyList<T>> QueryAsync(Query query, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(query);
            EnsureConnectionAndTransaction();

            // Abwärtskompatibel: ggf. alte Felder in neue Conditions überführen
            query.NormalizeLegacy();

            var cols = _mapper.GetPropertyMaps(typeof(T));
            if (cols.Count == 0)
                throw new InvalidOperationException($"No mapped columns found for {typeof(T).Name}.");

            var table = _dialect.QuoteIdentifier(_mapper.GetTableName(typeof(T)));
            var colList = string.Join(", ", cols.Select(c => _dialect.QuoteIdentifier(c.ColumnName)));

            var sb = new StringBuilder($"SELECT {colList} FROM {table}");
            using var cmd = _uow.Connection.CreateCommand();
            cmd.Transaction = _uow.Transaction;

            // WHERE (AND-verknüpft)
            if (query.Conditions.Count > 0)
            {
                sb.Append(" WHERE ");
                for (int i = 0; i < query.Conditions.Count; i++)
                {
                    var cond = query.Conditions[i];
                    var dbCol = ResolveColumnName(cond.Column);
                    var quotedCol = _dialect.QuoteIdentifier(dbCol);

                    // Operator-Mapping
                    string opSql = cond.Op switch
                    {
                        Operator.Eq => "=",
                        Operator.Gt => ">",
                        Operator.Ge => ">=",
                        Operator.Lt => "<",
                        Operator.Le => "<=",
                        _ => throw new NotSupportedException($"Unsupported operator {cond.Op}.")
                    };

                    if (i > 0) sb.Append(" AND ");

                    if (cond.Op == Operator.Eq && cond.Value is null)
                    {
                        // IS NULL für Equals(null)
                        sb.Append($"{quotedCol} IS NULL");
                    }
                    else
                    {
                        // Parameternamen: p0, p1, ...
                        var pName = $"p{i}";
                        sb.Append($"{quotedCol} {opSql} {_dialect.ParameterPrefix}{pName}");
                        AddParameter(cmd, pName, cond.Value);
                    }
                }
            }

            // ORDER BY
            if (!string.IsNullOrWhiteSpace(query.OrderByColumn))
            {
                var dbCol = ResolveColumnName(query.OrderByColumn);
                sb.Append(" ORDER BY ");
                sb.Append(_dialect.QuoteIdentifier(dbCol));
                sb.Append(query.OrderByDesc ? " DESC" : " ASC");
            }

            sb.Append(';');
            cmd.CommandText = sb.ToString();

            using var reader = await ExecuteReaderAsync(cmd, ct).ConfigureAwait(false);
            return MaterializeList(reader, cols);
        }

        // ---------- Interne Hilfsmethoden ----------

        /// <summary>
        /// Materialisiert eine Liste von Entitäten aus einem <see cref="IDataReader"/>
        /// anhand der Mapping-Informationen.
        /// </summary>
        /// <param name="reader">Geöffneter DataReader.</param>
        /// <param name="cols">Die gemappten Spalten/Properties.</param>
        /// <returns>Liste materialisierter Entitäten.</returns>
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

        /// <summary>
        /// Löst einen vom Aufrufer übergebenen Spalten-/Propertynamen zu einem gültigen DB-Spaltennamen auf.
        /// </summary>
        /// <param name="token">CLR-Propertyname oder DB-Spaltenname.</param>
        /// <returns>Der gemappte DB-Spaltenname.</returns>
        /// <exception cref="ArgumentException">Wenn weder Property- noch Spaltenname bekannt ist.</exception>
        private string ResolveColumnName(string token)
        {
            // Alle Mappings für T
            var maps = _mapper.GetPropertyMaps(typeof(T));

            // 1) exakter Treffer auf DB-Spaltennamen
            var m = maps.FirstOrDefault(x =>
                string.Equals(x.ColumnName, token, StringComparison.OrdinalIgnoreCase));
            if (m is not null) return m.ColumnName;

            // 2) Treffer auf CLR-Property
            m = maps.FirstOrDefault(x =>
                string.Equals(x.PropertyName, token, StringComparison.OrdinalIgnoreCase));
            if (m is not null) return m.ColumnName;

            // 3) Versuch: Translator auf den Token anwenden (falls jemand CLR eingibt),
            //    dann mit DB-Spaltennamen matchen (z. B. "FirstName" -> "first_name")
            var translated = _names.Column(token);
            m = maps.FirstOrDefault(x =>
                string.Equals(x.ColumnName, translated, StringComparison.OrdinalIgnoreCase));
            if (m is not null) return m.ColumnName;

            throw new ArgumentException(
                $"Unknown column/property '{token}' for entity {typeof(T).Name}. " +
                "Use a CLR property name or the mapped database column name.", nameof(token));
        }
    }
}
