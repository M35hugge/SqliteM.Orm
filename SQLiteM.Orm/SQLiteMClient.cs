using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;

namespace SQLiteM.Orm
{
    public class SQLiteMClient : IAsyncDisposable
    {
        private readonly ServiceProvider _sp;

        public SQLiteMClient(string connectionOrPath)
        {
            var cs = connectionOrPath.Contains('=')
                ? connectionOrPath
                : $"Data Source={connectionOrPath};Cache=Shared";

            _sp = new ServiceCollection()
                .AddSQLiteM(o => o.ConnectionString = cs)
                .BuildServiceProvider();
        }
        public Task EnsureCreateAsync<T>(CancellationToken ct = default)
                => EnsureCreatedAsync<T>(ct);

        /// <summary>Legt Tabelle für T an (falls nicht vorhanden). </summary>
        public async Task EnsureCreatedAsync<T>(CancellationToken ct= default)
        {
            var uowFactory = _sp.GetRequiredService<IUnitOfWorkFactory>();
            var builder = _sp.GetRequiredService<ISqlBuilder>();

            await using var uow = await uowFactory.CreateAsync(ct).ConfigureAwait(false);
            await SQLiteMBootstrap.EnsureCreatedAsync<T>(uow,builder,ct).ConfigureAwait(false);
            await uow.CommitAsync(ct).ConfigureAwait(false);
        }
        ///<summary>Legt Tabelle für die angegebenen Typen an (falls nicht vorhanden) in der angegebenen Reihenfolge. </summary>
        public async Task EnsureCreateAsync(CancellationToken ct = default, params Type[] entityTypes)
        {
            if(entityTypes==null  || entityTypes.Length == 0) return;

            var uowFactory = _sp.GetRequiredService<IUnitOfWorkFactory>();
            var builder= _sp.GetRequiredService<ISqlBuilder>(); ;

            await using var uow = await uowFactory.CreateAsync(ct).ConfigureAwait(false);
            await SQLiteMBootstrap.EnsureCreatedAsync(uow, builder, ct, entityTypes).ConfigureAwait(false);
            await uow.CommitAsync(ct).ConfigureAwait (false);
        }

        public async Task<long> InsertAsync<T>(T entity, CancellationToken ct=default) where T : class, new()
        {
            var uowFactory = _sp.GetRequiredService<IUnitOfWorkFactory>();
            var repoFactory= _sp.GetRequiredService<IRepositoryFactory>();
            await using var uow= await uowFactory.CreateAsync(ct).ConfigureAwait(false);

            var repo = repoFactory.Create<T>(uow);
            var rows = await repo.InsertAsync(entity, ct).ConfigureAwait(false);

            await uow.CommitAsync(ct).ConfigureAwait(false);
            return rows;
        }

        public async Task<int> UpdateAsync<T>(T entity, CancellationToken ct= default)where T : class, new()
        {
            var uowFactory = _sp.GetRequiredService<IUnitOfWorkFactory>();
            var repoFactory = _sp.GetRequiredService<IRepositoryFactory>();
            await using var uow = await uowFactory.CreateAsync(ct).ConfigureAwait(false);

            var repo = repoFactory.Create<T>(uow);
            var rows = await repo.UpdateAsync(entity, ct).ConfigureAwait(false);

            await uow.CommitAsync(ct).ConfigureAwait(false);
            return rows;
        }


        public async Task<int> DeleteAsync<T>(object id , CancellationToken ct = default) where T : class, new()
        {
            var uowFactory = _sp.GetRequiredService<IUnitOfWorkFactory>();
            var repoFactory =_sp.GetRequiredService<IRepositoryFactory>();
            await using var uow = await uowFactory.CreateAsync(ct).ConfigureAwait (false);

            var repo = repoFactory.Create<T>(uow);
            var rows = await repo.DeleteAsync(id, ct).ConfigureAwait(false);
            
            await uow.CommitAsync(ct).ConfigureAwait(false);
            return rows;
        }

        public async Task<T?>FindByIdAsync<T>(object id ,CancellationToken ct = default) where T : class, new()
        {
            var uowFactory = _sp.GetRequiredService<IUnitOfWorkFactory>();
            var repoFactory = _sp.GetRequiredService<IRepositoryFactory>();

            await using var uow = await uowFactory.CreateAsync(ct).ConfigureAwait((false));

            var repo = repoFactory.Create<T>(uow);
            var e= await repo.FindByIdAsync(id, ct).ConfigureAwait(false);
            await uow.CommitAsync(ct).ConfigureAwait(false);
            return e;
        }
        public async Task<IReadOnlyList<T>> FindAllAsync<T>(CancellationToken ct = default) where T : class, new()
        {
            var uowFactory = _sp.GetRequiredService<IUnitOfWorkFactory>();
            var repoFactory =_sp.GetRequiredService<IRepositoryFactory>();

            await using var uow = await uowFactory.CreateAsync(ct).ConfigureAwait(false);
            var repo=repoFactory.Create<T>(uow);    
            var list = await repo.FindAllAsync(ct).ConfigureAwait(false);
            return list;
        }

        public async Task<IReadOnlyList<T>> QueryAsync<T>(Query query, CancellationToken ct = default) where T : class, new()
        {
            var uowFactory=_sp.GetRequiredService<IUnitOfWorkFactory>();
            var repoFactory = _sp.GetRequiredService<IRepositoryFactory>();

            await using var uow = await uowFactory.CreateAsync(ct).ConfigureAwait (false);

            var repo = repoFactory.Create<T>(uow);

            var list = await repo.QueryAsync(query, ct).ConfigureAwait(false);
            await uow.CommitAsync(ct).ConfigureAwait(false);
            return list;
        }

        public async Task WithTransactionAsync(Func<IRepositoryFactory, IUnitOfWork, Task> action)
        {
            var uowFactory = _sp.GetRequiredService<IUnitOfWorkFactory>();
            var repoFactory = _sp.GetRequiredService<IRepositoryFactory>();
            await using var uow = await uowFactory.CreateAsync();
            await action(repoFactory, uow);
            await uow.CommitAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _sp.DisposeAsync();
        }


    }
}
