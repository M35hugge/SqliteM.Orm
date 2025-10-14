using SQLiteM.Abstractions;
using System.Data;
using System.Data.Common;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;

namespace SQLiteM.Orm
{
    public sealed class Repository<T>(IUnitOfWork uow, IEntityMapper mapper, ISqlBuilder builder, ISqlDialect dialect) : IRepository<T> where T : class, new()
    {
        private readonly IUnitOfWork _uow=uow;
        private readonly IEntityMapper _mapper = mapper;
        private readonly ISqlBuilder _sql = builder; 
        private readonly ISqlDialect dialect = dialect;

        public async Task<long> InsertAsync(T entity, CancellationToken ct = default)
        {
            var cmdText = _sql.BuildInsert(typeof(T), out var cols);
            using var cmd = _uow.Connection.CreateCommand();
            cmd.Transaction= _uow.Transaction;
            cmd.CommandText= cmdText;

            foreach (var c in cols)
            {
                var val = typeof(T).GetProperty(c.PropertyName)!.GetValue(entity);
                AddParameter(cmd, c.ColumnName, val);
            }
            await ExecuteNonQueryAsync(cmd, ct).ConfigureAwait(false);

            var key = _mapper.GetPrimaryKey(typeof(T));
            if(key is not null && key.IsAutoIncerement)
            {
                cmd.CommandText = "Select last_insert_rowid();";
                var id= await ExecuteScalarAsync(cmd, ct).ConfigureAwait(false);
                typeof(T).GetProperty(key.PropertyName)!.SetValue(entity, Convert.ToInt64(id));
                return Convert.ToInt64(id);
            }
            return 0;

        }
        public async Task<int> UpdateAsync(T entity, CancellationToken ct = default)
        {
            var cmdText= _sql.BuildUpdate(typeof(T),out var cols);
            using var cmd = _uow.Connection.CreateCommand();
            cmd.Transaction= _uow.Transaction;
            cmd.CommandText= cmdText;

            foreach (var c in cols)
            {
                var val= typeof(T).GetProperty(c.PropertyName) !.GetValue(entity);
                AddParameter(cmd, c.ColumnName, val);
            }
            var key = _mapper.GetPrimaryKey (typeof(T))!;
            var keyVal= typeof(T).GetProperty(key.PropertyName)!.GetValue(entity);
            AddParameter(cmd, key.ColumnName, keyVal);

            return await ExecuteNonQueryAsync(cmd, ct).ConfigureAwait(false);
        }

        public async Task<int> DeleteAsync(object id, CancellationToken ct = default)
        {
            var cmdText= _sql.BuildDelete(typeof(T),out var key);
            using var cmd = _uow.Connection.CreateCommand();
            cmd.Transaction= _uow.Transaction;
            cmd.CommandText= cmdText;
            AddParameter(cmd, key.ColumnName, id);
            return await ExecuteNonQueryAsync(cmd, ct).ConfigureAwait(false);
        }

        public async Task<T?> FindByIdAsync(object id, CancellationToken ct = default)
        {
            var cmdText=_sql.BuildSelectedById(typeof(T), out var key, out var cols);
            using var cmd = _uow.Connection.CreateCommand();
            cmd.Transaction = _uow.Transaction;
            cmd.CommandText= cmdText;
            AddParameter(cmd, key.ColumnName, id);

            using var reader = await ExecuteReaderAsync(cmd, ct).ConfigureAwait(false);
            if (!reader.Read()) return null;
            var entity = new T();
            foreach(var c in cols)
            {
                var prop = typeof(T).GetProperty(c.PropertyName, BindingFlags.Public | BindingFlags.Instance);
                if (prop is null) continue;

                var ordinal = reader.GetOrdinal(c.PropertyName);
                if(reader.IsDBNull(ordinal))
                {
                    prop.SetValue(entity, null);
                    continue;
                }
                object value = reader.GetValue(ordinal);
                prop.SetValue(entity, ConvertTo(prop.PropertyType, value));
            }
            return entity;

        }       

       

        private static void AddParameter(IDbCommand cmd, string name, object? value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName=name;
            p.Value=value ??DBNull.Value;
            cmd.Parameters.Add(p);
        }

        private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken ct)
        {
            return command switch
            {
                DbCommand dbCmd => await dbCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false),
                _ => command.ExecuteNonQuery()
            };
        }
        private static async Task<object> ExecuteScalarAsync(IDbCommand command, CancellationToken ct)
        {
            return command switch
            {
                DbCommand dbCmd => await dbCmd.ExecuteScalarAsync(ct).ConfigureAwait(false) ?? 0,
                _=> command.ExecuteScalar() ?? 0
            };
        }

        private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken ct)
        {
            return command switch
            {
                DbCommand dbCmd => await dbCmd.ExecuteReaderAsync(ct).ConfigureAwait(false),
                _ => command.ExecuteReader()
            };
        }

        private static object? ConvertTo(Type targetType, object value)
        {
            if(targetType ==typeof(string))return value.ToString();
            if(targetType ==typeof(int))return Convert.ToInt32(value);
            if(targetType ==typeof(long))return Convert.ToInt64(value);
            if(targetType ==typeof(bool))return Convert.ToInt64(value)!=0;
            if(targetType ==typeof(double))return Convert.ToDouble(value);
            if(targetType ==typeof(float))return Convert.ToSingle(value);
            if(targetType ==typeof(decimal))return Convert.ToDecimal(value);
            if(targetType ==typeof(DateTime))return DateTime.Parse(value.ToString()!);
            if(Nullable.GetUnderlyingType(targetType) is Type u)
            {
                return ConvertTo(u, value);
            }
            return value;
        }
    }
}
