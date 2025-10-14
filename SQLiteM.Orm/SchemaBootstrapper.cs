using SQLiteM.Abstractions;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteM.Orm
{
    public static class SchemaBootstrapper
    {
        public static async Task EnsureCreatedAsync<T>(IUnitOfWork uow, ISqlBuilder builder, CancellationToken ct = default)
        {
            var ddl=builder.BuildCreateTable(typeof(T));
            using var cmd = uow.Connection.CreateCommand();
            cmd.CommandText = ddl;
            if (cmd is DbCommand dbCmd)
            {
                await dbCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
            else
                cmd.ExecuteNonQuery();
        }
    }
}
