using SQLiteM.Abstractions;
using System.Reflection;

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
        /// <summary>
        /// Ermittelt den Tabellennamen für den angegebenen Entitätstyp.
        /// </summary>
        /// <param name="entityType">Der zu untersuchende Entitätstyp.</param>
        /// <returns>Der über <see cref="TableAttribute"/> konfigurierte Tabellenname.</returns>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="entityType"/> null ist.</exception>
        /// <exception cref="InvalidOperationException">
        /// Wenn der Entitätstyp kein <see cref="TableAttribute"/> besitzt oder der Name leer ist.
        /// </exception>
        public string GetTableName(Type entityType)
        {
            ArgumentNullException.ThrowIfNull(entityType);

            var table = entityType.GetCustomAttribute<TableAttribute>()
                ?? throw new InvalidOperationException($"TableAttribute is missing {entityType.Name}");

            if (string.IsNullOrWhiteSpace(table.Name))
                throw new InvalidOperationException($"TableAttribute.Name must not be empty on type {entityType.Name}.");

            return table.Name;
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
            ArgumentNullException.ThrowIfNull(entityType);

            var porps = entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var list = new List<PropertyMap>();

            foreach (var p in porps)
            {
                var col = p.GetCustomAttribute<ColumnAttribute>();
                if (col is null) continue;
                if(string.IsNullOrEmpty(col.Name)) throw new InvalidOperationException($"ColumnAttribute.Name must not be empty on property {entityType.Name}.{p.Name}");

                var isPrimaryKey = p.GetCustomAttribute<PrimaryKeyAttribute>() != null;
                var isAutoIncrement = p.GetCustomAttribute<AutoIncrementAttribute>() != null;

                list.Add(new PropertyMap(
                    ColumnName: col.Name,
                    PropertyName: p.Name,
                    PropertyType: p.PropertyType,
                    IsPrimaryKey: isPrimaryKey,
                    IsAutoIncrement: isAutoIncrement,
                    IsNullable: col.IsNullable,
                    Length: col.Length
                 ));
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
        public PropertyMap? GetPrimaryKey(Type entityType)
        {
            ArgumentNullException.ThrowIfNull(entityType);
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
            ArgumentNullException.ThrowIfNull(entityType);
            var props = entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var list = new List<ForeignKeyMap>();

            foreach (var p in props)
            {
                var fk = p.GetCustomAttribute<ForeignKeyAttribute>();
                var col = p.GetCustomAttribute<ColumnAttribute>();

                if (fk is null || col is null) continue;

                if (string.IsNullOrWhiteSpace(col.Name))
                    throw new InvalidOperationException($"ColumnAttribute.Name must not be empty or property {entityType.Name}.{p.Name}");


                var principalTable = GetTableName(fk.PrincipalEntity);

                if (string.IsNullOrWhiteSpace(fk.PrincipalColumn))
                    throw new InvalidOperationException($"ColumnAttribute.Name must not be empty or property {entityType.Name}.{p.Name}");

                list.Add(new ForeignKeyMap(
                    ThisColumn: col.Name,
                    PrincipalEntity: fk.PrincipalEntity,
                    PrincipalTable: principalTable,
                    PrincipalColumn: fk.PrincipalColumn,
                    OnDelete: fk.OnDelete
                ));
            }
            return list;
        }
    }
}
