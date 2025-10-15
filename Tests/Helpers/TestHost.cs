using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using SQLiteM.Orm;
using System;
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

        public static async Task WithUowAsync(ServiceProvider sp, Func<IUnitOfWork, Task> action)
        {
            await using var uow = await sp.GetRequiredService<IUnitOfWorkFactory>().CreateAsync();
            await action(uow);
        }
    }
}
