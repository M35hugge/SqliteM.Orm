using Microsoft.Data.Sqlite;
using SQLiteM.Abstractions;
using System.Data;

namespace SQLiteM.Orm.Internal
{
    /// <summary>
    /// Implementiert eine Factory zur Erstellung von <see cref="IUnitOfWork"/>-Instanzen.
    /// </summary>
    /// <remarks>
    /// Diese Factory kapselt die Erzeugung einer neuen <see cref="IUnitOfWork"/>-Instanz und
    /// stellt sicher, dass alle benötigten Verbindungen über die bereitgestellte
    /// <see cref="IConnectionFactory"/> erstellt werden.
    /// </remarks>
    internal class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly IConnectionFactory _factory;
        
        public UnitOfWorkFactory(IConnectionFactory factory)
        {
            ArgumentNullException.ThrowIfNull(factory);
            _factory = factory;
        }

        /// <summary>
        /// Erstellt asynchron eine neue <see cref="IUnitOfWork"/>-Instanz.
        /// </summary>
        /// <param name="ct">Optionaler <see cref="CancellationToken"/> (derzeit ohne Wirkung).</param>
        /// <returns>Die neu erstellte <see cref="IUnitOfWork"/>.</returns>
        /// <remarks>
        /// Die Verbindung wird im <see cref="SQLiteM.Orm.Internal.UnitOfWork"/> geöffnet,
        /// <c>PRAGMA foreign_keys = ON</c> gesetzt und eine Transaktion gestartet.
        /// Pro Transaktion/Scope eine neue UoW verwenden.
        /// </remarks>
        public Task<IUnitOfWork> CreateAsync(CancellationToken ct = default)
        {
            return Task.FromResult<IUnitOfWork>(new UnitOfWork(_factory));
        }
    }

    /// <summary>
    /// Implementiert eine Factory zur Erstellung typisierter Repository-Instanzen.
    /// </summary>
    /// <remarks>
    /// Diese Factory kapselt die Erstellung von <see cref="IRepository{T}"/>-Instanzen
    /// und stellt sicher, dass alle benötigten Abhängigkeiten (<see cref="IEntityMapper"/>,
    /// <see cref="ISqlBuilder"/>, <see cref="ISqlDialect"/>) bereitgestellt werden.
    /// </remarks>
    internal class RepositoryFactory: IRepositoryFactory
    {
        private readonly IEntityMapper _mapper;
        private readonly ISqlBuilder _sql;
        private readonly ISqlDialect _dialect;

        /// <summary>
        /// Initialisiert eine neue Instanz der <see cref="RepositoryFactory"/>.
        /// </summary>
        /// <param name="mapper">Der <see cref="IEntityMapper"/>.</param>
        /// <param name="builder">Der <see cref="ISqlBuilder"/>.</param>
        /// <param name="dialect">Der <see cref="ISqlDialect"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// Wenn <paramref name="mapper"/>, <paramref name="builder"/> oder <paramref name="dialect"/> null ist.
        /// </exception>
        public RepositoryFactory(IEntityMapper mapper, ISqlDialect dialect, ISqlBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(dialect);
            ArgumentNullException.ThrowIfNull(builder);
            _mapper = mapper;
            _dialect = dialect;
            _sql = builder;
        }

        /// <summary>
        /// Erstellt ein neues Repository, das an die übergebene <see cref="IUnitOfWork"/> gebunden ist.
        /// </summary>
        /// <typeparam name="T">Der Entitätstyp.</typeparam>
        /// <param name="uow">Die aktive <see cref="IUnitOfWork"/>.</param>
        /// <returns>Eine <see cref="IRepository{T}"/>-Instanz.</returns>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="uow"/> null ist.</exception>
        public IRepository<T> Create<T>(IUnitOfWork uow) where T : class, new()
        {
            ArgumentNullException.ThrowIfNull(uow);
            return new Repository<T>(uow, _mapper, _sql, _dialect);
        }
    }

    /// <summary>
    /// Factory zur Erstellung von SQLite-Verbindungen.
    /// </summary>
    /// <remarks>
    /// Erzeugt <see cref="SqliteConnection"/> auf Basis einer Verbindungszeichenfolge.
    /// Die Verbindung wird im geschlossenen Zustand zurückgegeben; Öffnen/Schließen obliegt dem Aufrufer.
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
        /// Erstellt eine neue Datenbankverbindung für SQLite.
        /// </summary>
        /// <returns>Eine offene <see cref="IDbConnection"/> für die SQLite-Datenbank.</returns>
        /// <remarks>
        /// Die Verbindung wird initial in geschlossenem Zustand zurückgegeben.
        /// Der Aufrufer ist dafür verantwortlich, sie zu öffnen und zu schließen.
        /// </remarks>
        public IDbConnection Create() => new SqliteConnection(_connectionString);
    }
}
