using SQLiteM.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteM.Orm
{
    public sealed class SqlBuilder(IEntityMapper mapper, ISqlDialect dialect) : ISqlBuilder
    {

        private readonly IEntityMapper _mapper = mapper;
        private readonly ISqlDialect _dialect= dialect;

        


        public string BuildInsert(Type entityType, out IReadOnlyList<PropertyMap> cols)
        {
            var all = _mapper.GetPropertyMaps(entityType);

            cols =[..all.Where(c => !c.IsAutoIncerement)];
            var table = _dialect.QuoteIdentifier(_mapper.GetTableName(entityType));
            var colList = string.Join(", ", cols.Select(c => _dialect.QuoteIdentifier(c.ColumnName)));
            var paramList= string.Join(", ", cols.Select(c=> _dialect.ParamerterPrefix + c.ColumnName));

            return $"INSERT INTO {table} ({colList}) VALUES ({paramList});";
        }

        public string BuildUpdate(Type entityType, out IReadOnlyList<PropertyMap> cols)
        {
            var all = _mapper.GetPropertyMaps(entityType);
            var key = all.FirstOrDefault(c => c.IsPrimaryKey)
                ?? throw new InvalidOperationException("PRIMARY KEY is missing");

            cols = [.. all.Where(c=>!c.IsPrimaryKey && !c.IsAutoIncerement)];
            var table = _dialect.QuoteIdentifier(_mapper.GetTableName(entityType));
            var sets = string.Join(", ", cols.Select(c
                => $"{_dialect.QuoteIdentifier(c.ColumnName)} = {_dialect.ParamerterPrefix}{c.ColumnName}"));

            return $"UPDATE {table} SET {sets} WHERE {_dialect.QuoteIdentifier(key.ColumnName)} = {_dialect.ParamerterPrefix}{key.ColumnName};";
        }

        public string BuildDelete(Type entityType, out PropertyMap key)
        {
            key = _mapper.GetPrimaryKey(entityType)
                ?? throw new InvalidOperationException("PRIMARY KEY is missing");

            var table = _dialect.QuoteIdentifier(_mapper.GetTableName(entityType));

            return $" DELETE FROM {table} WHERE {_dialect.QuoteIdentifier(key.ColumnName)} = {_dialect.ParamerterPrefix}{key.ColumnName};";
        }

        public string BuildSelectedById(Type entityType, out PropertyMap key, out IReadOnlyList<PropertyMap> cols)
        {
            cols = _mapper.GetPropertyMaps(entityType);
            key = cols.FirstOrDefault(c => c.IsPrimaryKey)
                ?? throw new InvalidOperationException("PRIMARY KEY is missing");

            var table = _dialect.QuoteIdentifier(_mapper.GetTableName(entityType));
            var colList = string.Join(", ", cols.Select(c => _dialect.QuoteIdentifier(c.ColumnName)));

            return $"SELECT {colList} FROM {table} WHERE {_dialect.QuoteIdentifier(key.ColumnName)} = {_dialect.ParamerterPrefix}{key.ColumnName} LIMIT 1;";
        }

        public string BuildCreateTable(Type entityType)
        {
            var table = _dialect.QuoteIdentifier(_mapper.GetTableName(entityType));
            var cols = _mapper.GetPropertyMaps(entityType);

            var sb = new StringBuilder();
            sb.Append($"CREATE TABLE IF NOT EXISTS {table} (");

            var defs = new List<string>();
            foreach (var col in cols)
            {
                var typeSql = ToSqlType(col);
                var pk = col.IsPrimaryKey ? " PRIMARY KEY" : string.Empty;
                var ai = col.IsAutoIncerement ? " AUTOINCREMENT" : string.Empty;
                var nn = col.IsNullable || col.IsPrimaryKey ? string.Empty : " NOT NULL";

                defs.Add($"{_dialect.QuoteIdentifier(col.ColumnName)} {typeSql}{pk}{ai}{nn}");
            }
            sb.Append(string.Join(", ", defs));
            sb.Append(");");
            return sb.ToString();


            static string ToSqlType(PropertyMap c)
            {
                var t = c.PropertyType;

                if (t == typeof(int) || t == typeof(long)) 
                    return "INTEGER";

                if (t == typeof(bool)) 
                    return "INTEGER";

                if (t == typeof(double) || t == typeof(float)) 
                    return "REAL";

                if (t == typeof(DateTime) || t == typeof(DateTimeOffset)) 
                    return "TEXT";

                if (t == typeof(string))
                    return c.Length > 0 ? $"VARCHAR({c.Length})" : "TEXT";

                return "TEXT";
            }
        }
    }
}
