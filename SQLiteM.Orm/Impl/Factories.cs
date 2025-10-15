using Microsoft.Data.Sqlite;
using SQLiteM.Abstractions;
using SQLiteM.Orm.Internal;
using System.Data;

namespace SQLiteM.Orm.Impl
{
    /// <summary>
    /// Implementiert eine Factory zur Erstellung von <see cref="IUnitOfWork"/>-Instanzen.
    /// </summary>
    /// <remarks>
    /// Diese Factory kapselt die Erzeugung einer neuen <see cref="IUnitOfWork"/>-Instanz und
    /// stellt sicher, dass alle benötigten Verbindungen über die bereitgestellte
    /// <see cref="IConnectionFactory"/> erstellt werden.
    /// </remarks>
    public class UnitOfWorkFactory(IConnectionFactory factory) : IUnitOfWorkFactory
    {
        private readonly IConnectionFactory _factory = factory;

        /// <summary>
        /// Erstellt asynchron eine neue <see cref="IUnitOfWork"/>-Instanz.
        /// </summary>
        /// <param name="ct">Ein optionales <see cref="CancellationToken"/> zur Steuerung des Vorgangs.</param>
        /// <returns>Eine <see cref="Task{TResult}"/> mit der erstellten <see cref="IUnitOfWork"/>.</returns>
        /// <remarks>
        /// Der Aufruf dieser Methode erstellt eine neue Arbeitseinheit, die eine Verbindung über die
        /// konfigurierte <see cref="IConnectionFactory"/> öffnet. 
        /// Sie ist in der Regel pro Transaktion oder Anfrage zu verwenden.
        /// </remarks>
        public Task<IUnitOfWork> CreateAsync(CancellationToken ct = default)
            => Task.FromResult<IUnitOfWork>(new UnitOfWork(_factory));
    }

    /// <summary>
    /// Implementiert eine Factory zur Erstellung typisierter Repository-Instanzen.
    /// </summary>
    /// <remarks>
    /// Diese Factory kapselt die Erstellung von <see cref="IRepository{T}"/>-Instanzen
    /// und stellt sicher, dass alle benötigten Abhängigkeiten (<see cref="IEntityMapper"/>,
    /// <see cref="ISqlBuilder"/>, <see cref="ISqlDialect"/>) bereitgestellt werden.
    /// </remarks>
    public class RepositoryFactory(IEntityMapper mapper, ISqlBuilder builder, ISqlDialect dialect) : IRepositoryFactory
    {
        private readonly IEntityMapper _mapper = mapper;
        private readonly ISqlBuilder _sql = builder;
        private readonly ISqlDialect _dialect = dialect;

        /// <summary>
        /// Erstellt ein neues, typisiertes Repository für den angegebenen Entitätstyp.
        /// </summary>
        /// <typeparam name="T">Der Entitätstyp, der durch das Repository verwaltet wird.</typeparam>
        /// <param name="uow">Die aktive <see cref="IUnitOfWork"/>, in der das Repository arbeitet.</param>
        /// <returns>Eine neue Instanz von <see cref="IRepository{T}"/>.</returns>
        /// <remarks>
        /// Das erstellte Repository ist an die übergebene Arbeitseinheit gebunden und nutzt deren
        /// Transaktionskontext. Dadurch wird sichergestellt, dass Änderungen koordiniert und
        /// konsistent gespeichert werden.
        /// </remarks>
        public IRepository<T> Create<T>(IUnitOfWork uow) where T : class, new()
            => new Repository<T>(uow, _mapper, _sql, _dialect);
    }

    /// <summary>
    /// Implementiert eine Factory zur Erstellung von SQLite-Verbindungen.
    /// </summary>
    /// <remarks>
    /// Diese Factory erzeugt neue <see cref="SqliteConnection"/>-Instanzen basierend
    /// auf der bereitgestellten Verbindungszeichenfolge.
    /// </remarks>
    public sealed class SqliteConnectionFactory(string connectionString) : IConnectionFactory
    {
        private readonly string _connectionString = connectionString;

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
