using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteM.Orm.Pub
{
    /// <summary>
    /// Stellt Konfigurationsoptionen für das SQLiteM-ORM bereit.
    /// </summary>
    /// <remarks>
    /// Diese Optionen werden bei der Registrierung über
    /// <see cref="ServiceCollectionExtensions"/>
    /// konfiguriert und bestimmen insbesondere die zu verwendende SQLite-Verbindung.
    /// </remarks>
    /// <example>
    /// Beispiel:
    /// <code language="csharp">
    /// services.AddSQLiteM(opt =&gt;
    /// {
    ///     opt.ConnectionString = "Data Source=app.db";
    /// });
    /// </code>
    /// </example>
    public sealed class SQLiteMOptions
    {
        /// <summary>
        /// Gibt die Verbindungszeichenfolge zur SQLite-Datenbank an.
        /// </summary>
        /// <value>
        /// Standardmäßig <c>"Data Source = :memory:"</c>, wodurch eine temporäre
        /// In-Memory-Datenbank verwendet wird.
        /// </value>
        /// <remarks>
        /// Diese Eigenschaft kann beim Initialisieren des Dienstcontainers überschrieben werden,
        /// um eine persistente Datei-Datenbank oder eine benannte In-Memory-Datenbank zu verwenden.
        /// Beispiel:
        /// <c>"Data Source=app.db"</c> oder <c>"Data Source=file::memory:?cache=shared"</c>.
        /// </remarks>
        public string ConnectionString { get; set; } = "Data Source = :memory:";

        public bool EnableWal { get; set; } = false;           // später per PRAGMA
#nullable enable
        public string? JournalMode { get; set; }               // z.B. "WAL"
        public string? Synchronous { get; set; }               // z.B. "NORMAL"
    }
}
