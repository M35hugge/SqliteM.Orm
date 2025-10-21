using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using SQLiteM.Orm.Pub;
using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace Tests.Helpers
{
    public static class TestHost
    {
        public static ServiceProvider CreateProvider(out string dbPath)
        {
            dbPath = Path.Combine(Path.GetTempPath(), $"sqliteM_test_{Guid.NewGuid():N}.db");
            var cs = $"Data Source={dbPath};Cache=Shared";

            var services = new ServiceCollection()
                .AddSQLiteM(o => o.ConnectionString = cs);

            return services.BuildServiceProvider();
        }
        public static ServiceProvider CreateProviderSharedMem()
        {
            var cs = "Data Source=file:sqlitem_tests?mode=memory&cache=shared";

            var keeper = new SqliteConnection(cs);
            keeper.Open(); // DB bleibt am Leben

            return new ServiceCollection()
                .AddSQLiteM(o => o.ConnectionString = cs)
                .AddSingleton<IDbConnection>(keeper) // optional, falls du sie injizieren willst
                .BuildServiceProvider();
        }
        public static async Task WithUowAsync(ServiceProvider sp, Func<IUnitOfWork, Task> action)
        {
            await using var uow = await sp.GetRequiredService<IUnitOfWorkFactory>().CreateAsync();
            await action(uow);
        }
        public static async Task<TResult> WithUowAsync<TResult>(IServiceProvider sp,Func<IUnitOfWork, Task<TResult>> action)
        {
            var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();
            await using var uow = await uowFactory.CreateAsync();
            var result = await action(uow);
            await uow.CommitAsync();
            return result;
        }
    }
}
