using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using SQLiteM.Orm.Pub;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Tests.Helpers
{
    public static class TestHost
    {
        private static string MakeDbPath(string? name = null)
        {
            name ??= GetCallingTestName();
            var safe = string.Concat(name.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
            return Path.Combine(Path.GetTempPath(), $"sqliteM_test_{safe}.db");
        }

        private static string GetCallingTestName()
        {
            var st = new StackTrace();
            foreach (var f in st.GetFrames() ?? Array.Empty<StackFrame>())
            {
                if (f.GetMethod() is MethodInfo m)
                {
                    var hasFact = m.GetCustomAttributes(inherit: true)
                                   .Any(a => a.GetType().Name is "FactAttribute" or "TheoryAttribute");
                    if (hasFact) return $"{m.DeclaringType?.Name}_{m.Name}";
                }
            }
            return $"Guid_{Guid.NewGuid():N}";
        }

        public static ServiceProvider CreateProvider() => CreateProvider(out _);

        public static ServiceProvider CreateProvider(out string dbPath)
        {
            dbPath = MakeDbPath();
            if (File.Exists(dbPath)) File.Delete(dbPath); // <<< wichtig
            var cs = $"Data Source={dbPath};Cache=Shared";

            return new ServiceCollection()
                .AddSQLiteM(o => o.ConnectionString = cs, _ => new SnakeCaseNameTranslator())
                .BuildServiceProvider();
        }

        public static ServiceProvider CreateProvider(INameTranslator translator)
            => CreateProvider(translator, out _);

        public static ServiceProvider CreateProvider(INameTranslator translator, out string dbPath)
        {
            dbPath = MakeDbPath();
            if (File.Exists(dbPath)) File.Delete(dbPath); // <<< wichtig
            var cs = $"Data Source={dbPath};Cache=Shared";

            return new ServiceCollection()
                .AddSQLiteM(o => o.ConnectionString = cs, _ => translator)
                .BuildServiceProvider();
        }

        public static async Task WithUowAsync(ServiceProvider sp, Func<IUnitOfWork, Task> action)
        {
            await using var uow = await sp.GetRequiredService<IUnitOfWorkFactory>().CreateAsync();
            await action(uow);
        }

        public static async Task<TResult> WithUowAsync<TResult>(IServiceProvider sp, Func<IUnitOfWork, Task<TResult>> action)
        {
            var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();
            await using var uow = await uowFactory.CreateAsync();
            var result = await action(uow);
            await uow.CommitAsync();
            return result;
        }
    }
}