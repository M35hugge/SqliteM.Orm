using SQLiteM.Abstractions;
using System.Reflection;
using System.Xml.Linq;

namespace SQLiteM.Orm.Pub
{
    /// <summary>
    /// Reflexionsbasierte Implementierung von <see cref="IEntityMapper"/>, die
    /// Entitäten über Attribute den Tabellen- und Spaltennamen zuordnet.
    /// </summary>
    /// <remarks>
    /// Dieser Mapper berücksichtigt ausschließlich öffentliche Instanz-Properties.
    /// Ein Property wird nur dann gemappt, wenn es mit <see cref="ColumnAttribute"/> annotiert ist.
    /// Der Tabellenname wird über <see cref="TableAttribute"/> an der Entitätsklasse bestimmt.
    /// Primärschlüssel und Auto-Inkrement werden über <see cref="PrimaryKeyAttribute"/> bzw.
    /// <see cref="AutoIncrementAttribute"/> erkannt. Fremdschlüssel werden über
    /// <see cref="ForeignKeyAttribute"/> am Property abgeleitet.
    /// </remarks>
    /// <seealso cref="IEntityMapper"/>
    /// <seealso cref="TableAttribute"/>
    /// <seealso cref="ColumnAttribute"/>
    /// <seealso cref="PrimaryKeyAttribute"/>
    /// <seealso cref="AutoIncrementAttribute"/>
    /// <seealso cref="ForeignKeyAttribute"/>
    /// <seealso cref="PropertyMap"/>
    /// <seealso cref="ForeignKeyMap"/>
    public sealed class ReflectionEntityMapper : IEntityMapper
    {

        private readonly INameTranslator _names;



        /// <summary>
        /// Erstellt einen Mapper mit Namensübersetzer.
        /// </summary>
#nullable enable
        public ReflectionEntityMapper(INameTranslator? translator = null)
        {
            _names = translator ?? throw new ArgumentNullException(nameof(translator));
        }

        /// <summary>
        /// Ermittelt den Tabellennamen für den angegebenen Entitätstyp. 
        /// </summary>
        /// <param name="entityType">Der zu untersuchende Entitätstyp.</param>
        /// <returns>Der über <see cref="TableAttribute"/> konfigurierte Tabellenname.
        /// Wenn nicht explizit angegeben wird entityType.Name angenommen;</returns>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="entityType"/> null ist.</exception>
        /// <exception cref="InvalidOperationException">
        /// Wenn der Entitätstyp kein <see cref="TableAttribute"/>besitzt.
        /// </exception>
        public string GetTableName(Type entityType)
        {
            ArgumentNullException.ThrowIfNull(entityType);

            var tableAttr = entityType.GetCustomAttribute<TableAttribute>();
            var tableName = string.IsNullOrWhiteSpace(tableAttr?.Name) ? entityType.Name : tableAttr!.Name;

            // Nur Fallback (kein Attributname) durch TABLE-Übersetzer schicken
            if (string.IsNullOrWhiteSpace(tableAttr?.Name))
                tableName = _names.Table(tableName);

            return tableName;
        }

        /// <summary>
        /// Ermittelt die Property-Zuordnungen (Spaltenmapping) für den angegebenen Entitätstyp.
        /// </summary>
        /// <param name="entityType">Der zu untersuchende Entitätstyp.</param>
        /// <returns>
        /// Eine schreibgeschützte Liste von <see cref="PropertyMap"/>-Einträgen für alle
        /// öffentlichen Instanz-Properties mit <see cref="ColumnAttribute"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="entityType"/> null ist.</exception>
        /// <remarks>
        /// Ein Property wird nur dann in die Ergebnisliste aufgenommen, wenn es mit
        /// <see cref="ColumnAttribute"/> versehen ist. Primärschlüssel und Auto-Inkrement
        /// werden anhand von <see cref="PrimaryKeyAttribute"/> bzw. <see cref="AutoIncrementAttribute"/>
        /// erkannt und in der <see cref="PropertyMap"/> gespiegelt.
        /// </remarks>
        public IReadOnlyList<PropertyMap> GetPropertyMaps(Type entityType)
        {            
            var porps = entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var list = new List<PropertyMap>();

            foreach (var p in porps)
            {
                if (p.GetCustomAttribute<IgnoreAttribute>() is not null)
                    continue;
                var col = p.GetCustomAttribute<ColumnAttribute>();

                // Konvention: Wenn kein [Column] vorhanden, trotzdem mappen
                // (Du kannst das auf "nur mit [Column]" einschränken, wenn gewünscht)

                var dbColumn = string.IsNullOrWhiteSpace(col?.Name)
                ? _names.Column(p.Name)
                : col!.Name;

                var isPrimaryKey = p.GetCustomAttribute<PrimaryKeyAttribute>() is not null
                    // Konvention: "Id" oder "<TypeName>Id"
                    || string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(p.Name, entityType.Name +"Id", StringComparison.OrdinalIgnoreCase);


                var isAuto = p.GetCustomAttribute<AutoIncrementAttribute>() is not null;

                var idx = p.GetCustomAttribute<IndexAttribute>();
                var isIndex = idx is not null;
                var isUniqueIndex = idx?.IsUnique ?? false;

                // IsNullable-Fallback: wenn kein Column-Attribut vorhanden und Type ist ValueType ohne Nullable<>
                var isNullable = col?.IsNullable ?? IsNullableByType(p.PropertyType);
                var length = col?.Length ?? 0;

                var isUniqueColumn = col?.IsUniqueColumn ?? false;

                list.Add(new PropertyMap(
                    ColumnName: dbColumn,
                    PropertyName: p.Name,
                    PropertyType: p.PropertyType,
                    IsPrimaryKey: isPrimaryKey,
                    IsAutoIncrement: isAuto,
                    IsIndex: isIndex,
                    IsUniqueIndex: isUniqueIndex,
                    IsUniqueColumn: isUniqueColumn,
                    IsNullable: isNullable,
                    Length: length
                 ));
            }

            var dup = list
                .GroupBy(m => m.ColumnName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);

            if (dup is not null)
            {
                var names = string.Join(", ", dup.Select(m => m.PropertyName));
                throw new InvalidOperationException(
                    $"Column name collision after translation: '{dup.Key}' is produced by properties [{names}] on type {entityType.Name}.");
            }
            return list;
        }

        /// <summary>
        /// Ermittelt die <see cref="PropertyMap"/> des Primärschlüssels für den angegebenen Entitätstyp.
        /// </summary>
        /// <param name="entityType">Der zu untersuchende Entitätstyp.</param>
        /// <returns>
        /// Die Primärschlüssel-Zuordnung oder <see langword="null"/>, wenn kein
        /// Property mit <see cref="PrimaryKeyAttribute"/> vorhanden ist.
        /// </returns>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="entityType"/> null ist.</exception>
#nullable enable
        public PropertyMap? GetPrimaryKey(Type entityType)
        {
            return GetPropertyMaps(entityType).FirstOrDefault(m => m.IsPrimaryKey);
        }

        /// <summary>
        /// Ermittelt alle Fremdschlüssel-Zuordnungen für den angegebenen Entitätstyp.
        /// </summary>
        /// <param name="entityType">Der zu untersuchende Entitätstyp.</param>
        /// <returns>
        /// Eine schreibgeschützte Liste von <see cref="ForeignKeyMap"/>-Einträgen
        /// für Properties, die sowohl mit <see cref="ForeignKeyAttribute"/> als auch
        /// mit <see cref="ColumnAttribute"/> versehen sind.
        /// </returns>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="entityType"/> null ist.</exception>
        /// <exception cref="InvalidOperationException">
        /// Wenn ein <see cref="ForeignKeyAttribute"/> eine Principal-Entität ohne <see cref="TableAttribute"/>
        /// oder mit leerem Tabellennamen referenziert.
        /// </exception>
        /// <remarks>
        /// Der Name der referenzierten Tabelle wird mittels <see cref="GetTableName(Type)"/>
        /// aus dem im <see cref="ForeignKeyAttribute.PrincipalEntity"/> angegebenen Entitätstyp
        /// abgeleitet. Die <c>ON DELETE</c>-Aktion entspricht dem Wert im Attribut
        /// (<see cref="OnDeleteAction"/>).
        /// </remarks>
        public IReadOnlyList<ForeignKeyMap> GetForeignKeys(Type entityType)
        {
            var props = entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var fks = new List<ForeignKeyMap>();

            foreach (var p in props)
            {
                var fk = p.GetCustomAttribute<ForeignKeyAttribute>();
                var col = p.GetCustomAttribute<ColumnAttribute>();
                if (fk is null) continue;

                var thisColumn =string.IsNullOrWhiteSpace(col?.Name) 
                    ? _names.Column(p.Name)
                    : col!.Name;

                var principalTable = GetTableName(fk.PrincipalEntity);

                fks.Add(new ForeignKeyMap(
                    ThisColumn: thisColumn,
                    PrincipalEntity: fk.PrincipalEntity,
                    PrincipalTable: GetTableName(fk.PrincipalEntity),
                    PrincipalColumn: fk.PrincipalColumn,
                    OnDelete: fk.OnDelete
                ));
            }
            return fks;
        }

        public IReadOnlyList<IndexMap> GetIndexes(Type entityType)
        {
            var props = entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var maps = GetPropertyMaps(entityType);
            var byProp = maps.ToDictionary(m => m.PropertyName, m => m, StringComparer.Ordinal);

            var result = new List<IndexMap>();

            // Einzelspalten-Indizes aus [Index] an Properties
            foreach (var p in props)
            {
                var idx = p.GetCustomAttribute<IndexAttribute>();
                if (idx is null) continue;

                if (!byProp.TryGetValue(p.Name, out var pm))
                    continue;

                result.Add(new IndexMap(
                    Name: idx.Name, // kann null sein
                    Columns: new[] { pm.ColumnName },
                    IsUnique: idx.IsUnique
                ));
            }

            // Composite-Indizes aus Klassen-Attributen
            foreach (var comp in entityType.GetCustomAttributes<CompositeIndexAttribute>())
            {
                if (comp.Columns.Length == 0) continue;

                var colNames = comp.Columns.Select(propName =>
                {
                    // Property -> DB-Spaltenname
                    if (!byProp.TryGetValue(propName, out var pm))
                        throw new InvalidOperationException(
                            $"CompositeIndex references unknown property '{propName}' on {entityType.Name}.");
                    return pm.ColumnName;
                }).ToArray();

                result.Add(new IndexMap(
                    Name: comp.Name,
                    Columns: colNames,
                    IsUnique: comp.IsUnique
                ));
            }

            return result;
        }

        private static bool IsNullableByType(Type t)
        {
            if (!t.IsValueType) return true;                // Referenztypen: nullable
            return Nullable.GetUnderlyingType(t) != null;   // Nullable<T>
        }
    }
}
