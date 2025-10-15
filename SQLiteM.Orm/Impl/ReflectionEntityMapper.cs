using SQLiteM.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteM.Orm.Impl
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
        /// <exception cref="InvalidOperationException">
        /// Wird ausgelöst, wenn der Entitätstyp kein <see cref="TableAttribute"/> besitzt.
        /// </exception>
        public string GetTableName(Type entityType)
        {
            var table = entityType.GetCustomAttribute<TableAttribute>()
                ?? throw new InvalidOperationException($"TableAttribute is missing {entityType.Name}");
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
                var col = p.GetCustomAttribute<ColumnAttribute>();
                if (col is null) continue;

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
        public PropertyMap? GetPrimaryKey(Type entityType) => GetPropertyMaps(entityType).FirstOrDefault(m => m.IsPrimaryKey);

        /// <summary>
        /// Ermittelt alle Fremdschlüssel-Zuordnungen für den angegebenen Entitätstyp.
        /// </summary>
        /// <param name="entityType">Der zu untersuchende Entitätstyp.</param>
        /// <returns>
        /// Eine schreibgeschützte Liste von <see cref="ForeignKeyMap"/>-Einträgen
        /// für Properties, die sowohl mit <see cref="ForeignKeyAttribute"/> als auch
        /// mit <see cref="ColumnAttribute"/> versehen sind.
        /// </returns>
        /// <remarks>
        /// Der Name der referenzierten Tabelle wird mittels <see cref="GetTableName(Type)"/>
        /// aus dem im <see cref="ForeignKeyAttribute.PrincipalEntity"/> angegebenen Entitätstyp
        /// abgeleitet. Die <c>ON DELETE</c>-Aktion entspricht dem Wert im Attribut
        /// (<see cref="OnDeleteAction"/>).
        /// </remarks>
        public IReadOnlyList<ForeignKeyMap> GetForeignKeys(Type entityType)
        {
            var props = entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var list = new List<ForeignKeyMap>();

            foreach (var p in props)
            {
                var fk = p.GetCustomAttribute<ForeignKeyAttribute>();
                var col = p.GetCustomAttribute<ColumnAttribute>();

                if (fk is null || col is null) continue;

                var principalTable = GetTableName(fk.PrincipalEntity);
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
