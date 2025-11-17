using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using SQLiteM.Orm.Internal;

namespace SQLiteM.Orm.Pub
{
    /// <summary>
    /// Erweiterungsmethoden zur Registrierung der SQLiteM-Komponenten im DI-Container.
    /// </summary>
    /// <remarks>
    /// Die Methode
    /// <see cref="ServiceCollectionExtensions.AddSQLiteM(Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Action{SQLiteM.Orm.Pub.SQLiteMOptions}, System.Func{System.IServiceProvider, SQLiteM.Abstractions.INameTranslator})" />
    /// registriert die zentralen ORM-Dienste (Dialekt, Mapper, SQL-Builder, Verbindungs-/Repository-Fabriken)
    /// als <c>Singleton</c>s. Die Verbindungszeichenfolge wird über <see cref="SQLiteMOptions"/> konfiguriert.
    /// </remarks>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Fügt alle benötigten SQLiteM-Dienste zum Dienstcontainer hinzu.
        /// </summary>
        /// <param name="services">Die zu erweiternde <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">Konfiguration der <see cref="SQLiteMOptions"/> (z. B. Connection-String).</param>
        /// <param name="translatorFactory">
        /// Optionaler Factory-Delegat, der einen <see cref="INameTranslator"/> bereitstellt (z. B. Snake Case).
        /// Wird keiner angegeben, wird <c>IdentityNameTranslator</c> verwendet.
        /// </param>
        /// <returns>Die aktualisierte <see cref="IServiceCollection"/> zur weiteren Verkettung.</returns>
        /// <remarks>
        /// Registrierte Dienste (alle mit Lebensdauer <c>Singleton</c>):
        /// <list type="bullet">
        /// <item><description><see cref="ISqlDialect"/> → <see cref="SqliteDialect"/></description></item>
        /// <item><description><see cref="INameTranslator"/> → Identity oder benutzerdefiniert</description></item>
        /// <item><description><see cref="IEntityMapper"/> → <see cref="ReflectionEntityMapper"/></description></item>
        /// <item><description><see cref="ISqlBuilder"/> → <see cref="SqlBuilder"/></description></item>
        /// <item><description><see cref="IConnectionFactory"/> → <see cref="SqliteConnectionFactory"/></description></item>
        /// <item><description><see cref="IUnitOfWorkFactory"/> → <see cref="UnitOfWorkFactory"/></description></item>
        /// <item><description><see cref="IRepositoryFactory"/> → <see cref="RepositoryFactory"/></description></item>
        /// </list>
        /// Hinweis: Eine <see cref="IUnitOfWork"/> wird typischerweise pro Transaktion erzeugt
        /// (via <see cref="IUnitOfWorkFactory"/>).
        /// </remarks>
#nullable enable
        public static IServiceCollection AddSQLiteM(
            this IServiceCollection services, Action<SQLiteMOptions> configure, Func<IServiceProvider, INameTranslator>? translatorFactory = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);

            var options = new SQLiteMOptions();
            configure(options);

            // Infrastruktur
            services.AddSingleton<IConnectionFactory>(_ => new SqliteConnectionFactory(options.ConnectionString));
            services.AddSingleton<ISqlDialect, SqliteDialect>();

            // Name-Translator VOR dem Mapper (damit der Mapper ihn injiziert bekommt)
            if (translatorFactory is null)
            {
                services.AddSingleton<INameTranslator, IdentityNameTranslator>();
            }
            else
            {
                services.AddSingleton<INameTranslator>(translatorFactory);
            }
            // Mapper (kann INameTranslator im Ctor annehmen)
            services.AddSingleton<IEntityMapper, ReflectionEntityMapper>();

            // SQL-Builder & Factories
            services.AddSingleton<ISqlBuilder, SqlBuilder>();
            services.AddSingleton<IUnitOfWorkFactory, UnitOfWorkFactory>();
            services.AddSingleton<IRepositoryFactory, RepositoryFactory>();



            return services;
        }
    }
}
