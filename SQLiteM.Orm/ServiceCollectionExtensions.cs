using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteM.Orm
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSQLiteM(
            this IServiceCollection services, Action<SQLiteMOptions> configure)
        {
            var options = new SQLiteMOptions();
            configure(options);

            services.AddSingleton<ISqlDialect, SqliteDialect>();
            services.AddSingleton<IEntityMapper, ReflectionEntityMapper>();
            services.AddSingleton<ISqlBuilder, SqlBuilder>();

            services.AddSingleton<IConnectionFactory>(_ => new SqliteConnectionFactory(options.ConnectionString));
            services.AddSingleton<IUnitOfWorkFactory, UnitOfWorkFactory>();
            services.AddSingleton<IRepositoryFactory, RepositoryFactory>();

            return services;
        }
    }
}
