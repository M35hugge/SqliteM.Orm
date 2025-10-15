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

        private bool _completed;

        /// <summary>
        /// Erstellt eine neue <see cref="UnitOfWork"/>-Instanz,
        /// öffnet die Datenbankverbindung, aktiviert Fremdschlüsselprüfungen
        /// und startet eine neue Transaktion.
        /// </summary>
        /// <param name="factory">Die <see cref="IConnectionFactory"/>, die die Verbindung erzeugt.</param>
        public UnitOfWork(IConnectionFactory factory)
        {
            Connection = factory.Create();
            Connection.Open();

            // Fremdschlüsselprüfung aktivieren (SQLite-spezifisch)
            using (var pragma = Connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA foreign_keys = ON;";
                pragma.ExecuteNonQuery();
            }

            Transaction = Connection.BeginTransaction();
        }

        /// <summary>
        /// Bestätigt die aktuelle Transaktion und schließt die Arbeitseinheit ab.
        /// </summary>
        /// <param name="ct">Ein optionales <see cref="CancellationToken"/>.</param>
        /// <returns>Eine abgeschlossene Aufgabe, sobald der Commit erfolgt ist.</returns>
        /// <remarks>
        /// Nach dem Commit ist die <see cref="UnitOfWork"/> abgeschlossen und sollte
        /// nicht mehr für weitere Datenbankoperationen verwendet werden.
        /// </remarks>
        public Task CommitAsync(CancellationToken ct = default)
        {
            if (_completed) return Task.CompletedTask;
            Transaction.Commit();
            _completed = true;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Bricht die aktuelle Transaktion ab und verwirft alle Änderungen.
        /// </summary>
        /// <param name="ct">Ein optionales <see cref="CancellationToken"/>.</param>
        /// <returns>Eine abgeschlossene Aufgabe, sobald der Rollback erfolgt ist.</returns>
        /// <remarks>
        /// Nach einem Rollback ist die <see cref="UnitOfWork"/> abgeschlossen und 
        /// sollte nicht mehr weiterverwendet werden.
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
            if (!_completed)
            {
                try { await RollbackAsync().ConfigureAwait(false); }
                catch { /* Fehler beim Rollback werden unterdrückt */ }
                finally
                {
                    Transaction.Dispose();
                    Connection.Close();
                    Connection.Dispose();
                }
            }
        }
    }
}
