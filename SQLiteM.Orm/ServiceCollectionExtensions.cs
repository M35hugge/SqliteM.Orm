using System;
using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using SQLiteM.Orm.Impl;
using SQLiteM.Orm.Internal;

namespace SQLiteM.Orm
{
    /// <summary>
    /// Erweiterungsmethoden zur Registrierung der SQLiteM-Komponenten im DI-Container.
    /// </summary>
    /// <remarks>
    /// Die Methode <see cref="AddSQLiteM(IServiceCollection, Action{SQLiteMOptions})"/> registriert
    /// die zentralen ORM-Dienste (Dialekt, Mapper, SQL-Builder, Verbindungs-/Repository-Fabriken)
    /// als <c>Singleton</c>s. Die Verbindungszeichenfolge wird über <see cref="SQLiteMOptions"/>
    /// konfiguriert.
    /// </remarks>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Fügt alle benötigten SQLiteM-Dienste zum Dienstcontainer hinzu.
        /// </summary>
        /// <param name="services">Die zu erweiternde <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">Ein Delegat zur Konfiguration der <see cref="SQLiteMOptions"/> (z. B. Connection-String).</param>
        /// <returns>Die aktualisierte <see cref="IServiceCollection"/> zur weiteren Verkettung.</returns>
        /// <remarks>
        /// Registrierte Dienste (alle mit Lebensdauer <c>Singleton</c>):
        /// <list type="bullet">
        /// <item><description><see cref="ISqlDialect"/> → <see cref="SqliteDialect"/></description></item>
        /// <item><description><see cref="IEntityMapper"/> → <see cref="ReflectionEntityMapper"/></description></item>
        /// <item><description><see cref="ISqlBuilder"/> → <see cref="SqlBuilder"/></description></item>
        /// <item><description><see cref="IConnectionFactory"/> → <see cref="SqliteConnectionFactory"/></description></item>
        /// <item><description><see cref="IUnitOfWorkFactory"/> → <see cref="UnitOfWorkFactory"/></description></item>
        /// <item><description><see cref="IRepositoryFactory"/> → <see cref="RepositoryFactory"/></description></item>
        /// </list>
        /// <para>
        /// Hinweis: Eine <see cref="IUnitOfWork"/> wird typischerweise pro Anforderung/Transaktion erzeugt.
        /// Verwende dazu die <see cref="IUnitOfWorkFactory"/> innerhalb des Anwendungsflows.
        /// </para>
        /// <example>
        /// Beispiel (Minimal):
        /// <code language="csharp">
        /// services.AddSQLiteM(opt =&gt; opt.ConnectionString = "Data Source=app.db");
        /// </code>
        /// </example>
        /// </remarks>
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
