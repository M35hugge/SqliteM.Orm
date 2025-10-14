using SQLiteM.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteM.Orm
{
    public sealed class ReflectionEntityMapper : IEntityMapper
    {
        public string GetTableName(Type entityType)
        {
            var table = entityType.GetCustomAttribute<TableAttribute>()
                ?? throw new InvalidOperationException($" TableAttribute is missing {entityType.Name}");
            return table.Name;
        }

       

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
                    PropertyName: col.Name,
                    PropertyType: p.PropertyType,
                    IsPrimaryKey: isPrimaryKey,
                    IsAutoIncerement: isAutoIncrement,
                    IsNullable: col.IsNullable,
                    Length:col.Length
                 ));
            }
            return list;
        }

        public PropertyMap? GetPrimaryKey(Type entityType)=> GetPropertyMaps(entityType).FirstOrDefault(m=>m.IsPrimaryKey);
       
    }
}
