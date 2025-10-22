using Microsoft.Data.Sqlite;
using SQLiteM.Abstractions;
using System.Data;

namespace SQLiteM.Orm.Internal
{
    /// <summary>
    /// Factory zur Erstellung von <see cref="IUnitOfWork"/>-Instanzen (Transaktions-Scopes).
    /// </summary>
    /// <remarks>
    /// Öffnet keine Verbindung selbst; das übernimmt die konkrete <see cref="UnitOfWork"/>.
    /// Pro Transaktion/Scope sollte eine neue <see cref="IUnitOfWork"/> erstellt werden.
    /// </remarks>
    internal class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly IConnectionFactory _factory;

        /// <summary>
        /// Initialisiert die Factory.
        /// </summary>
        /// <param name="factory">Die <see cref="IConnectionFactory"/> zur Erstellung von Verbindungen.</param>
        /// <exception cref="ArgumentNullException"><paramref name="factory"/> ist <c>null</c>.</exception>
        public UnitOfWorkFactory(IConnectionFactory factory)
        {
            ArgumentNullException.ThrowIfNull(factory);
            _factory = factory;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Die erzeugte <see cref="UnitOfWork"/> öffnet die Verbindung, aktiviert
        /// <c>PRAGMA foreign_keys = ON</c> und startet eine Transaktion.
        /// </remarks>
        public Task<IUnitOfWork> CreateAsync(CancellationToken ct = default)
        {
            return Task.FromResult<IUnitOfWork>(new UnitOfWork(_factory));
        }
    }

    /// <summary>
    /// Factory zur Erstellung typisierter Repositories.
    /// </summary>
    /// <remarks>
    /// Kapselt die Übergabe aller benötigten Abhängigkeiten an <see cref="Repository{T}"/>.
    /// </remarks>
    internal class RepositoryFactory: IRepositoryFactory
    {
        private readonly IEntityMapper _mapper;
        private readonly ISqlBuilder _sql;
        private readonly ISqlDialect _dialect;
        private readonly INameTranslator _translator;

        /// <summary>
        /// Initialisiert die Factory.
        /// </summary>
        /// <param name="mapper">Der <see cref="IEntityMapper"/>.</param>
        /// <param name="dialect">Der <see cref="ISqlDialect"/>.</param>
        /// <param name="builder">Der <see cref="ISqlBuilder"/>.</param>
        /// <param name="translator">Der <see cref="INameTranslator"/> (für Konventionsnamen).</param>
        /// <exception cref="ArgumentNullException">
        /// Ausgelöst, wenn ein Parameter <c>null</c> ist.
        /// </exception>
        public RepositoryFactory(IEntityMapper mapper, ISqlDialect dialect, ISqlBuilder builder, INameTranslator translator)
        {
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(dialect);
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(translator);
            _mapper = mapper;
            _dialect = dialect;
            _sql = builder;
            _translator = translator;

        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="uow"/> ist <c>null</c>.</exception>
        public IRepository<T> Create<T>(IUnitOfWork uow) where T : class, new()
        {
            ArgumentNullException.ThrowIfNull(uow);
            return new Repository<T>(uow, _mapper, _sql, _dialect, _translator);
        }
    }

    /// <summary>
    /// Factory zur Erstellung von SQLite-Datenbankverbindungen.
    /// </summary>
    /// <remarks>
    /// Gibt eine **geschlossene** <see cref="SqliteConnection"/> zurück. Öffnen/Schließen liegt in der Verantwortung des Aufrufers.
    /// </remarks>
    internal sealed class SqliteConnectionFactory : IConnectionFactory
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initialisiert eine neue Instanz der <see cref="SqliteConnectionFactory"/>.
        /// </summary>
        /// <param name="connectionString">Die SQLite-Connection-String (z. B. <c>Data Source=app.db;Cache=Shared</c>).</param>
        /// <exception cref="ArgumentException">Wenn <paramref name="connectionString"/> null oder leer ist.</exception>
        public SqliteConnectionFactory(string connectionString)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(connectionString);
            _connectionString = connectionString;
        }


        /// <summary>
        /// Erstellt eine neue, **geschlossene** SQLite-Verbindung.
        /// </summary>
        /// <returns>Eine geschlossene <see cref="IDbConnection"/>.</returns>
        public IDbConnection Create() => new SqliteConnection(_connectionString);
    }
}
