using SQLiteM.Abstractions;
using System.Data;

namespace SQLiteM.Orm.Internal;

/// <summary>
/// Implementierung des Unit-of-Work-Patterns, die eine geöffnete Verbindung
/// und eine aktive Transaktion kapselt.
/// </summary>
/// <remarks>
/// Beim Erzeugen der Instanz wird die Verbindung geöffnet,
/// die konfigurierten PRAGMAs (z. B. <c>foreign_keys = ON</c>, <c>journal_mode = WAL</c>)
/// ausgeführt und eine neue Transaktion gestartet.
/// Eine <see cref="UnitOfWork"/> repräsentiert damit einen Transaktions-Scope:
/// Alle darin ausgeführten Datenbankoperationen werden atomar bestätigt oder verworfen.
/// Nach einem Aufruf von <see cref="CommitAsync(CancellationToken)"/> oder
/// <see cref="RollbackAsync(CancellationToken)"/> gilt die Instanz als abgeschlossen.
/// </remarks>
/// <seealso cref="IUnitOfWork"/>
/// <seealso cref="IConnectionFactory"/>
internal sealed class UnitOfWork : IUnitOfWork, IAsyncDisposable
{

    private bool _completed;
    private bool _disposed;

    private readonly PragmaOptions _pragmaOptions;

    /// <summary>
    /// Die aktive Datenbankverbindung dieser Arbeitseinheit.
    /// </summary>
    /// <inheritdoc />
    public IDbConnection Connection { get; }

    /// <summary>
    /// Die aktuelle Transaktion, in der alle Operationen ausgeführt werden.
    /// </summary>
    /// <inheritdoc />
    public IDbTransaction Transaction { get; private set; }


    /// <summary>
    /// Erstellt eine neue <see cref="UnitOfWork"/>-Instanz,
    /// öffnet die Datenbankverbindung, wendet PRAGMAs an
    /// und startet eine neue Transaktion.
    /// </summary>
    /// <param name="factory">Die <see cref="IConnectionFactory"/>, die die Verbindung erzeugt.</param>
    /// <param name="pragmaOptions">PRAGMA-Konfiguration, die auf die Verbindung angewendet wird.</param>
    /// <exception cref="ArgumentNullException">Wenn <paramref name="factory"/> oder <paramref name="pragmaOptions"/> null ist.</exception>
    /// <exception cref="InvalidOperationException">Wenn Verbindung oder Transaktion nicht initialisiert werden kann.</exception>
    public UnitOfWork(IConnectionFactory factory, PragmaOptions pragmaOptions)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(pragmaOptions);

        _pragmaOptions = pragmaOptions;

        try
        {
            Connection = factory.Create() ?? throw new InvalidOperationException("IConnectionFactory.Create() returned null.");
            Connection.Open();

            ApplyPragmas(Connection, _pragmaOptions);

            Transaction = Connection.BeginTransaction() ?? throw new InvalidOperationException("Failed to begin transaction");
        }
        catch
        {
            try
            {
                Connection?.Close();
                Connection?.Dispose();
            }
            catch { }
            throw;
        }
    }

    /// <summary>
    /// Wendet die konfigurierten PRAGMAs auf die geöffnete Verbindung an.
    /// </summary>
    private static void ApplyPragmas(IDbConnection connection, PragmaOptions options)
    {
        using var cmd = connection.CreateCommand();

        if (options.JournalMode is not null)
        {
            cmd.CommandText = $"PRAGMA journal_mode = {options.JournalMode.Value.ToString().ToUpperInvariant()};";
            cmd.ExecuteNonQuery();
        }

        if (options.Synchronous is not null)
        {
            cmd.CommandText = $"PRAGMA synchronous = {options.Synchronous.Value.ToString().ToUpperInvariant()};";
            cmd.ExecuteNonQuery();
        }

        if (options.ForeignKeys is not null)
        {
            cmd.CommandText = $"PRAGMA foreign_keys = {(options.ForeignKeys.Value ? "ON" : "OFF")};";
            cmd.ExecuteNonQuery();
        }

        if (options.BusyTimeout is not null)
        {
            cmd.CommandText = $"PRAGMA busy_timeout = {options.BusyTimeout.Value};";
            cmd.ExecuteNonQuery();
        }

        if (options.CacheSize is not null)
        {
            cmd.CommandText = $"PRAGMA cache_size = {options.CacheSize.Value};";
            cmd.ExecuteNonQuery();
        }

        if (options.PageSize is not null)
        {
            cmd.CommandText = $"PRAGMA page_size = {options.PageSize.Value};";
            cmd.ExecuteNonQuery();
        }

        if (options.AdditionalPragmas is not null)
        {
            foreach (var raw in options.AdditionalPragmas)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                cmd.CommandText = raw;
                cmd.ExecuteNonQuery();
            }
        }
    }


    /// <summary>
    /// Bestätigt die aktuelle Transaktion und schließt die Arbeitseinheit ab.
    /// </summary>
    /// <param name="ct">Ein optionales <see cref="CancellationToken"/>. (Nicht verwendet)</param>
    /// <returns>Eine abgeschlossene Aufgabe, sobald der Commit erfolgt ist.</returns>
    /// <remarks>
    /// Nach dem Commit ist die <see cref="UnitOfWork"/> abgeschlossen und sollte
    /// nicht mehr für weitere Datenbankoperationen verwendet werden.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Wenn die Instanz bereits entsorgt wurde.</exception>
    public Task CommitAsync(CancellationToken ct = default)
    {
        EnsureNotDisposed();

        if (_completed) return Task.CompletedTask;

        Transaction.Commit();
        _completed = true;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gibt Ressourcen frei und rollt ggf. eine offene Transaktion zurück.
    /// </summary>
    /// <returns>
    /// Eine <see cref="ValueTask"/>, die den asynchronen Abschluss des Dispose-Vorgangs darstellt.
    /// </returns>
    /// <remarks>
    /// Falls weder <see cref="CommitAsync(CancellationToken)"/> noch
    /// <see cref="RollbackAsync(CancellationToken)"/> zuvor aufgerufen wurde,
    /// führt die Methode automatisch ein Rollback durch, um Konsistenz zu gewährleisten.
    /// Diese Methode ist idempotent und kann mehrfach aufgerufen werden.
    /// </remarks>
    public Task RollbackAsync(CancellationToken ct = default)
    {
        if (_completed) return Task.CompletedTask;
        Transaction.Rollback();
        _completed = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gibt Ressourcen frei und rollt ggf. eine offene Transaktion zurück.
    /// </summary>
    /// <returns>
    /// Eine <see cref="ValueTask"/>, die den asynchronen Abschluss des Dispose-Vorgangs darstellt.
    /// </returns>
    /// <remarks>
    /// Falls weder <see cref="CommitAsync(CancellationToken)"/> noch 
    /// <see cref="RollbackAsync(CancellationToken)"/> zuvor aufgerufen wurde, 
    /// führt die Methode automatisch ein Rollback durch, um Konsistenz zu gewährleisten.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        try
        {
            if (!_completed)
                try
                {
                    await RollbackAsync().ConfigureAwait(false);
                }
                catch { }
        }
        finally
        {
            try { Transaction?.Dispose(); } catch { }
            try
            {
                if (Connection != null)
                {
                    try { Connection.Close(); } catch { }
                    Connection.Dispose();

                }
            }
            catch { }
            _disposed = true;
        }
    }
    private void EnsureNotDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UnitOfWork), "The UnitOfWork has already been disposed");
    }
}
