using SQLiteM.Abstractions;

namespace SQLiteM.Orm.Internal;

/// <summary>
/// Kapselt einen Transaktionskontext (Unit of Work + Repository-Fabrik) und
/// stellt bequeme Methoden zum Arbeiten innerhalb einer Transaktion bereit.
/// </summary>
/// <remarks>
/// <para>
/// Über <see cref="Repo{T}"/> erhältst du ein typisiertes Repository, das an die
/// zugrunde liegende <see cref="IUnitOfWork"/> gebunden ist.
/// </para>
/// <para>
/// Der Kontext ist „commit-or-rollback“-exklusiv: Nach <see cref="CommitAsync"/> oder
/// <see cref="RollbackAsync"/> ist er abgeschlossen (<see cref="IsCompleted"/>) und sollte
/// nicht weiterverwendet werden. Beim <see cref="DisposeAsync"/> wird – falls noch nicht
/// abgeschlossen – automatisch ein Rollback angestoßen (Best-Effort) und die
/// <see cref="IUnitOfWork"/> entsorgt.
/// </para>
/// </remarks>
internal class TransactionContext : ITransactionContext
{

    private readonly IRepositoryFactory _repos;
    private bool _disposed;

    /// <summary>
    /// Die aktive <see cref="IUnitOfWork"/>, an die alle Repositories gebunden sind.
    /// </summary>
    public IUnitOfWork Uow { get; }

    /// <summary>
    /// Gibt an, ob die Transaktion bereits abgeschlossen wurde
    /// (durch <see cref="CommitAsync"/> oder <see cref="RollbackAsync"/>).
    /// </summary>
    public bool IsCompleted { get; private set; }


    /// <summary>
    /// Initialisiert eine neue Instanz von <see cref="TransactionContext"/>.
    /// </summary>
    /// <param name="uow">Die zu verwendende <see cref="IUnitOfWork"/>.</param>
    /// <param name="repos">Die <see cref="IRepositoryFactory"/> zur Erzeugung typisierter Repositories.</param>
    /// <exception cref="ArgumentNullException">
    /// Wenn <paramref name="uow"/> oder <paramref name="repos"/> <see langword="null"/> ist.
    /// </exception>
    public TransactionContext(IUnitOfWork uow, IRepositoryFactory repos)
    {
        ArgumentNullException.ThrowIfNull(uow);
        ArgumentNullException.ThrowIfNull(repos);
        (Uow, _repos) = (uow, repos);
    }


    /// <summary>
    /// Liefert ein typisiertes Repository, das mit dieser Transaktion verknüpft ist.
    /// </summary>
    /// <typeparam name="T">Der Entitätstyp.</typeparam>
    /// <returns>Ein <see cref="IRepository{T}"/> für den angegebenen Typ.</returns>
    /// <exception cref="ObjectDisposedException">Wenn der Kontext bereits entsorgt wurde.</exception>
    public IRepository<T> Repo<T>() where T : class, new()
    {
        EnsureNotDisposed();
        if (IsCompleted)
            throw new InvalidOperationException("The Transaction context is already completed. Create a new transaction to continue");

        return _repos.Create<T>(Uow);
    }

    /// <summary>
    /// Bestätigt die Transaktion und markiert den Kontext als abgeschlossen.
    /// </summary>
    /// <param name="ct">Ein optionaler <see cref="CancellationToken"/>.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach erfolgreichem Commit.</returns>
    /// <exception cref="ObjectDisposedException">Wenn der Kontext bereits entsorgt wurde.</exception>
    public async Task CommitAsync(CancellationToken ct = default)
    {
        EnsureNotDisposed();
        if (IsCompleted) return;

        await Uow.CommitAsync(ct).ConfigureAwait(false);
        IsCompleted = true;

    }

    /// <summary>
    /// Bricht die Transaktion ab (Rollback) und markiert den Kontext als abgeschlossen.
    /// </summary>
    /// <param name="ct">Ein optionaler <see cref="CancellationToken"/>.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach erfolgreichem Rollback.</returns>
    /// <exception cref="ObjectDisposedException">Wenn der Kontext bereits entsorgt wurde.</exception>
    public async Task RollbackAsync(CancellationToken ct = default)
    {
        EnsureNotDisposed();
        if (IsCompleted) return;

        await Uow.RollbackAsync(ct).ConfigureAwait(false);
        IsCompleted = true;
    }

    /// <summary>
    /// Entsorgt den Kontext. Falls die Transaktion noch nicht abgeschlossen ist,
    /// wird best-effort ein Rollback durchgeführt. Die zugrunde liegende
    /// <see cref="IUnitOfWork"/> wird stets entsorgt.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!IsCompleted)
        {
            try { await Uow.RollbackAsync().ConfigureAwait(false); }
            catch { }
            finally
            {
                try { await Uow.DisposeAsync().ConfigureAwait(false); }
                catch { }
                _disposed = true;
            }
        }
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
            ObjectDisposedException.ThrowIf(_disposed, "The transaction context has already been disposed.");
    }
}
