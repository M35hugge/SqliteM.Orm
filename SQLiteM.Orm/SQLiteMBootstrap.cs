using SQLiteM.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteM.Orm
{
    public static class SQLiteMBootstrap
    {
        public static async Task EnsureCreatedAsync<T>(
       IUnitOfWorkFactory uowFactory,
       ISqlBuilder builder,
       CancellationToken ct = default)
        {
            await using var uow = await uowFactory.CreateAsync(ct);
            await SchemaBootstrapper.EnsureCreatedAsync<T>(uow, builder, ct);
            await uow.CommitAsync(ct);
        }
    }
}
