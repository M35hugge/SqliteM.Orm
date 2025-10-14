using SQLiteM.Abstractions;

namespace SQLiteM.Orm;

public class UnitOfWorkFactory(IConnectionFactory factory) :IUnitOfWorkFactory
{
    private readonly IConnectionFactory _factory=factory;

    public Task<IUnitOfWork> CreateAsync(CancellationToken ct = default) => Task.FromResult<IUnitOfWork>(new UnitOfWork(_factory));

    
}

public class RepositoryFactory(IEntityMapper mapper, ISqlBuilder builder, ISqlDialect dialect) : IRepositoryFactory
{
    private readonly IEntityMapper _mapper=mapper;
    private readonly ISqlBuilder _sql=builder;
    private readonly ISqlDialect _dialect=dialect;

    public IRepository<T> Create<T>(IUnitOfWork uow) where T : class, new() 
        => new Repository<T>(uow, _mapper, _sql, _dialect);
}