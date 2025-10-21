using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using SQLiteM.Abstractions;

namespace SQLiteM.Orm.Internal
{
    /// <summary>
    /// Implementierung des Unit-of-Work-Patterns, die eine geöffnete Verbindung
    /// und eine aktive Transaktion kapselt.
    /// </summary>
    /// <remarks>
    /// Beim Erzeugen der Instanz wird die Verbindung geöffnet,
    /// <c>PRAGMA foreign_keys = ON</c> ausgeführt und eine neue Transaktion gestartet.
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
        /// öffnet die Datenbankverbindung, aktiviert Fremdschlüsselprüfungen
        /// und startet eine neue Transaktion.
        /// </summary>
        /// <param name="factory">Die <see cref="IConnectionFactory"/>, die die Verbindung erzeugt.</param>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="factory"/> null ist.</exception>
        /// <exception cref="InvalidOperationException">Wenn Verbindung oder Transaktion nicht initialisiert werden kann.</exception>
        public UnitOfWork(IConnectionFactory factory)
        {
            ArgumentNullException.ThrowIfNull(factory);
            try
            {
                Connection = factory.Create() ?? throw new InvalidOperationException("IConnectionFactory.Create() returned null.");
                Connection.Open();

                using (var pragma = Connection.CreateCommand())
                {
                    pragma.CommandText = "PRAGMA foreign_keys = ON;";
                    pragma.ExecuteNonQuery();
                }



                Transaction = Connection.BeginTransaction()?? throw new InvalidOperationException("Failed to begin transaction");
            }
            catch
            {
                try
                {
                    Connection?.Close();
                    Connection?.Dispose();
                }
                catch {}
                throw;
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
            if(_disposed) return;
            try
            {
                if(!_completed)
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
}
