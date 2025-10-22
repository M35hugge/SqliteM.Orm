using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using SQLiteM.Orm.Internal;

namespace SQLiteM.Orm.Pub
{
    /// <summary>
    /// Öffentliche, minimalistische Fassade über DI/Repositories/UnitOfWork.
    /// Erlaubt CRUD-Operationen, einfache Abfragen sowie transaktionale Scopes,
    /// indem lediglich ein Connection-String oder Pfad übergeben wird.
    /// </summary>
    public sealed class SQLiteMClient : IAsyncDisposable
    {
        private readonly ServiceProvider _sp;

        /// <summary>
        /// Erstellt einen neuen Client.
        /// </summary>
        /// <param name="connectionOrPath">
        /// Entweder eine vollständige Connection-String (enthält '=') oder ein Dateipfad;
        /// bei Pfad wird automatisch <c>Data Source=&lt;Pfad&gt;;Cache=Shared</c> gebildet.
        /// </param>
        /// <param name="nameFormat">
        /// Gibt bei bei Tabellenerstellung StringCase mit ("sc"= snake_case)
        /// </param>
        /// <exception cref="ArgumentException">Wenn <paramref name="connectionOrPath"/> null/leer ist.</exception>
        public SQLiteMClient(string connectionOrPath, string nameFormat="in")
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(connectionOrPath);

            
            var cs = connectionOrPath.Contains('=')
                ? connectionOrPath
                : $"Data Source={connectionOrPath};Cache=Shared";
            if (!string.IsNullOrWhiteSpace(nameFormat))
            {
                _sp = new ServiceCollection()
                .AddSQLiteM(o => o.ConnectionString = cs, sp => new SnakeCaseNameTranslator())
                .BuildServiceProvider();
            }
            else
            {
                _sp = new ServiceCollection()
                .AddSQLiteM(o => o.ConnectionString = cs)
                .BuildServiceProvider();
            }
            
        }

        // ---------------------------------------------------------------------
        // Bootstrapping (DDL)
        // ---------------------------------------------------------------------

        /// <summary>Alias für <see cref="EnsureCreatedAsync{T}(CancellationToken)"/>.</summary>
        public Task EnsureCreateAsync<T>(CancellationToken ct = default)
            => EnsureCreatedAsync<T>(ct);

        /// <summary>Legt Tabelle für T an (falls nicht vorhanden). </summary>
        public async Task EnsureCreatedAsync<T>(CancellationToken ct = default)
        {
            var uowFactory = _sp.GetRequiredService<IUnitOfWorkFactory>();
            var builder = _sp.GetRequiredService<ISqlBuilder>();

            await using var uow = await uowFactory.CreateAsync(ct).ConfigureAwait(false);
            await SQLiteMBootstrap.EnsureCreatedAsync<T>(uow, builder, ct).ConfigureAwait(false);
            await uow.CommitAsync(ct).ConfigureAwait(false);
        }
        ///<summary>Legt Tabelle für die angegebenen Typen an (falls nicht vorhanden) in der angegebenen Reihenfolge. </summary>
        public async Task EnsureCreateAsync(CancellationToken ct = default, params Type[] entityTypes)
        {
            if (entityTypes == null || entityTypes.Length == 0) return;

            var uowFactory = _sp.GetRequiredService<IUnitOfWorkFactory>();
            var builder = _sp.GetRequiredService<ISqlBuilder>(); ;

            await using var uow = await uowFactory.CreateAsync(ct).ConfigureAwait(false);
            await SQLiteMBootstrap.EnsureCreatedAsync(uow, builder, ct, entityTypes).ConfigureAwait(false);
            await uow.CommitAsync(ct).ConfigureAwait(false);
        }


        // ---------------------------------------------------------------------
        // Convenience: Ein-Schritt-Schreib-APIs, die explizit „Transaction“ im Namen tragen
        // (nutzen intern WithTransactionAsync).
        // ---------------------------------------------------------------------


        public Task<int> InsertTransactionAsync<T>(T e, CancellationToken ct = default) where T : class, new()
            => WithTransactionAsync(async tx 
                => await tx.Repo<T>().InsertAsync(e, ct), ct);

        public Task<int> UpdateTransactionAsync<T>(T e, CancellationToken ct = default) where T : class, new()
            => WithTransactionAsync(async tx 
                => await tx.Repo<T>().UpdateAsync(e, ct), ct);

        public Task<int> DeleteTransactionAsync<T>(object id, CancellationToken ct = default) where T : class, new()
            => WithTransactionAsync(async tx 
                => await tx.Repo<T>().DeleteAsync(id, ct), ct);
#nullable enable
        public Task<T?> FindByIdTransactionAsync<T>(object id, CancellationToken ct = default) where T : class, new()
            => WithReadOnlyAsync(async tx 
                => await tx.Repo<T>().FindByIdAsync(id, ct), ct);

        public Task<IReadOnlyList<T>> FindAllTransactionAsync<T>(CancellationToken ct = default) where T : class, new()
            => WithReadOnlyAsync(async tx 
                => await tx.Repo<T>().FindAllAsync(ct), ct);

        public Task<IReadOnlyList<T>> QueryTransactionAsync<T>(Query q, CancellationToken ct = default) where T : class, new()
            => WithReadOnlyAsync(async tx 
                => await tx.Repo<T>().QueryAsync(q, ct), ct);

        // ---------------------------------------------------------------------
        // CRUD (Auto-Transaction für Schreib-Operationen)
        // ---------------------------------------------------------------------

        /// <summary>Fügt eine Entität ein (eigene Transaktion; Commit on success).</summary>
        public async Task<int> InsertAsync<T>(T entity, CancellationToken ct=default) where T : class, new()
        {
            ArgumentNullException.ThrowIfNull(entity);

            var uowFactory = _sp.GetRequiredService<IUnitOfWorkFactory>();
            var repoFactory= _sp.GetRequiredService<IRepositoryFactory>();
            await using var uow= await uowFactory.CreateAsync(ct).ConfigureAwait(false);

            var repo = repoFactory.Create<T>(uow);
            var rows = await repo.InsertAsync(entity, ct).ConfigureAwait(false);

            await uow.CommitAsync(ct).ConfigureAwait(false);
            return rows;
        }
        /// <summary>Aktualisiert eine Entität (eigene Transaktion; Commit on success).</summary>
        public async Task<int> UpdateAsync<T>(T entity, CancellationToken ct= default)where T : class, new()
        {
            ArgumentNullException.ThrowIfNull(entity);

            var uowFactory = _sp.GetRequiredService<IUnitOfWorkFactory>();
            var repoFactory = _sp.GetRequiredService<IRepositoryFactory>();
            await using var uow = await uowFactory.CreateAsync(ct).ConfigureAwait(false);

            var repo = repoFactory.Create<T>(uow);
            var rows = await repo.UpdateAsync(entity, ct).ConfigureAwait(false);

            await uow.CommitAsync(ct).ConfigureAwait(false);
            return rows;
        }
        /// <summary>Löscht eine Entität anhand des Primärschlüssels (eigene Transaktion; Commit on success).</summary>
        public async Task<int> DeleteAsync<T>(object id , CancellationToken ct = default) where T : class, new()
        {
            ArgumentNullException.ThrowIfNull(id);

            var uowFactory = _sp.GetRequiredService<IUnitOfWorkFactory>();
            var repoFactory =_sp.GetRequiredService<IRepositoryFactory>();
            await using var uow = await uowFactory.CreateAsync(ct).ConfigureAwait (false);

            var repo = repoFactory.Create<T>(uow);
            var rows = await repo.DeleteAsync(id, ct).ConfigureAwait(false);
            
            await uow.CommitAsync(ct).ConfigureAwait(false);
            return rows;
        }
        /// <summary>Lädt eine Entität per Primärschlüssel (eigener Read-Only-Scope).</summary>
        public async Task<T?>FindByIdAsync<T>(object id ,CancellationToken ct = default) where T : class, new()
        {
            ArgumentNullException.ThrowIfNull(id);

            var uowFactory = _sp.GetRequiredService<IUnitOfWorkFactory>();
            var repoFactory = _sp.GetRequiredService<IRepositoryFactory>();

            await using var uow = await uowFactory.CreateAsync(ct).ConfigureAwait(false);

            var repo = repoFactory.Create<T>(uow);
            var e= await repo.FindByIdAsync(id, ct).ConfigureAwait(false);
            await uow.CommitAsync(ct).ConfigureAwait(false);
            return e;
        }
        /// <summary>Lädt alle Entitäten aus der Tabelle (eigener Read-Only-Scope).</summary>
        public async Task<IReadOnlyList<T>> FindAllAsync<T>(CancellationToken ct = default) where T : class, new()
        {
            var uowFactory = _sp.GetRequiredService<IUnitOfWorkFactory>();
            var repoFactory =_sp.GetRequiredService<IRepositoryFactory>();

            await using var uow = await uowFactory.CreateAsync(ct).ConfigureAwait(false);
            var repo=repoFactory.Create<T>(uow);    
            var list = await repo.FindAllAsync(ct).ConfigureAwait(false);
            return list;
        }
        /// <summary>Führt eine einfache parametrisierte Abfrage aus (eigener Read-Only-Scope).</summary>
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

        // ---------------------------------------------------------------------
        // Komfort: Schreib-Transaktion (Callback)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Führt benutzerdefinierte Arbeit in einer Transaktion aus und committet automatisch.
        /// </summary>
        public async Task WithTransactionAsync(Func<ITransactionContext, Task> work, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(work);

            await using var tx = await BeginTransactionAsync(ct).ConfigureAwait(false);
            await work(tx).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Führt benutzerdefinierte Arbeit in einer Transaktion aus, liefert ein Ergebnis und committet automatisch.
        /// </summary>
        public async Task<TResult> WithTransactionAsync<TResult>(Func<ITransactionContext, Task<TResult>> work, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(work);

            await using var tx = await BeginTransactionAsync(ct).ConfigureAwait(false);
            var result = await work(tx).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Führt benutzerdefinierte Arbeit in einem Read-Only-Transaktionskontext aus (kein Commit).
        /// </summary>
        public async Task<TResult> WithReadOnlyAsync<TResult>(Func<ITransactionContext, Task<TResult>> work, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(work);

            await using var tx = await BeginTransactionAsync(ct).ConfigureAwait(false);
            var result = await work(tx).ConfigureAwait(false);
            // Kein Commit (reiner Lese-Scope)
            return result;
        }

        // ---------------------------------------------------------------------
        // Low-level: Transaktion manuell beginnen
        // ---------------------------------------------------------------------

        /// <summary>
        /// Beginnt eine neue Transaktion und gibt einen <see cref="ITransactionContext"/> zurück.
        /// </summary>
        public async Task<ITransactionContext> BeginTransactionAsync(CancellationToken ct = default)
        {
            var uowFactory = _sp.GetRequiredService<IUnitOfWorkFactory>();
            var repoFactory = _sp.GetRequiredService<IRepositoryFactory>();
            var uow = await uowFactory.CreateAsync(ct).ConfigureAwait(false);
            return new TransactionContext(uow, repoFactory);
        }

        // ---------------------------------------------------------------------
        // Dispose
        // ---------------------------------------------------------------------

        /// <summary>
        /// Gibt den internen DI-Container frei.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await _sp.DisposeAsync();
            GC.SuppressFinalize(this);

        }
    }
}
