using System.Data;

namespace SQLiteM.Abstractions
{
    public interface IConnectionFactory
    {
        IDbConnection Create();
    }

    public interface ISqlDialect
    {
        string QuoteIdentifier(string name);
        string ParamerterPrefix { get; }
    }

    public interface IEntityMapper
    {
        string GetTableName(Type entityType);
        IReadOnlyList<PropertyMap>GetPropertyMaps(Type entityType);
        PropertyMap? GetPrimaryKey(Type entityType);
    }

    public interface ISqlBuilder
    {
        string BuildInsert(Type entityType, out IReadOnlyList<PropertyMap> cols);
        string BuildUpdate(Type entityType, out IReadOnlyList<PropertyMap> cols);
        string BuildDelete(Type entityType, out PropertyMap key);
        string BuildSelectedById(Type entityType, out PropertyMap key, out IReadOnlyList<PropertyMap> cols);
        string BuildCreateTable(Type entityType);
    }

    public interface IRepositoryFactory
    {
        IRepository<T> Create<T>(IUnitOfWork uow) where T : class, new();
    }

    public interface IRepository<T> where T : class, new()
    {
        Task<long>InsertAsync(T entity, CancellationToken ct = default);
        Task<int> UpdateAsync(T entity, CancellationToken ct = default);
        Task<int> DeleteAsync(object id, CancellationToken ct = default);
        Task<T?>  FindByIdAsync(object id, CancellationToken ct = default);
    }

    public interface IUnitOfWorkFactory
    {
        Task<IUnitOfWork> CreateAsync(CancellationToken ct = default);
    }

    public interface IUnitOfWork : IAsyncDisposable
    {
        IDbConnection Connection { get; }
        IDbTransaction Transaction { get; }
        Task CommitAsync(CancellationToken ct = default);
        Task RollbackAsync(CancellationToken ct = default);
    }

    //Helper
    public sealed record PropertyMap(string ColumnName, string PropertyName, Type PropertyType, bool IsPrimaryKey, bool IsAutoIncerement, bool IsNullable, int Length);
}
