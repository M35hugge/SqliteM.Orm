namespace SQLiteM.Abstractions
{
    /// <summary>
    /// Konfiguration für SQLite PRAGMA-Einstellungen, die pro Verbindung gesetzt werden sollen.
    /// </summary>
    public sealed class PragmaOptions
    {
        /// <summary>
        /// Zu verwendender Journal-Mode (z.B. WAL).
        /// </summary>
        public JournalMode? JournalMode { get; init; }

        /// <summary>
        /// Synchronous-Level.
        /// </summary>
        public SynchronousMode? Synchronous { get; init; }

        /// <summary>
        /// Aktiviert/deaktiviert Foreign-Key-Unterstützung (<c>PRAGMA foreign_keys</c>).
        /// </summary>
        public bool? ForeignKeys { get; init; }

        /// <summary>
        /// Busy-Timeout in Millisekunden (<c>PRAGMA busy_timeout</c>).
        /// </summary>
        public int? BusyTimeout { get; init; }

        /// <summary>
        /// Cache-Größe (<c>PRAGMA cache_size</c>).
        /// </summary>
        public int? CacheSize { get; init; }

        /// <summary>
        /// Seitengröße (<c>PRAGMA page_size</c>).
        /// </summary>
        public int? PageSize { get; init; }

        /// <summary>
        /// Zusätzliche rohe PRAGMA-Statements, die unverändert ausgeführt werden.
        /// </summary>
        public IReadOnlyList<string> AdditionalPragmas { get; init; } =
            Array.Empty<string>();

        /// <summary>
        /// Standard-Settings, die dem bisherigen Verhalten entsprechen:
        /// Foreign Keys = ON.
        /// </summary>
        public static PragmaOptions Default { get; } = new PragmaOptions
        {
            ForeignKeys = true
        };
    }
    /// <summary>
    /// Verfügbare Journal-Modi für <c>PRAGMA journal_mode</c>.
    /// </summary>
    public enum JournalMode
    {
        /// <summary>
        /// Standardmodus: Das Journal wird nach Abschluss einer Transaktion gelöscht (<c>DELETE</c>).
        /// </summary>
        Delete,

        /// <summary>
        /// Der Inhalt der Journal-Datei wird abgeschnitten, die Datei selbst aber beibehalten (<c>TRUNCATE</c>).
        /// </summary>
        Truncate,

        /// <summary>
        /// Die Journal-Datei bleibt bestehen, nur der Header wird zurückgesetzt (<c>PERSIST</c>).
        /// </summary>
        Persist,

        /// <summary>
        /// Das Journal wird ausschließlich im Speicher gehalten und nicht als Datei angelegt (<c>MEMORY</c>).
        /// </summary>
        Memory,

        /// <summary>
        /// Write-Ahead-Logging-Modus (<c>WAL</c>) für bessere Parallelität von Lese- und Schreibzugriffen.
        /// </summary>
        Wal,

        /// <summary>
        /// Journalisierung ist deaktiviert (<c>OFF</c>); erhöhtes Risiko von Datenverlust bei Abstürzen.
        /// </summary>
        Off
    }

    /// <summary>
    /// Verfügbare Synchronisations-Level für <c>PRAGMA synchronous</c>.
    /// </summary>
    public enum SynchronousMode
    {
        /// <summary>
        /// Keine Synchronisation (<c>OFF</c>); maximale Performance, geringste Datensicherheit.
        /// </summary>
        Off,

        /// <summary>
        /// Standard-Synchronisation (<c>NORMAL</c>); Kompromiss zwischen Performance und Sicherheit.
        /// </summary>
        Normal,

        /// <summary>
        /// Volle Synchronisation (<c>FULL</c>); sicherer gegen Datenverlust, aber langsamer.
        /// </summary>
        Full,

        /// <summary>
        /// Erhöhte Synchronisation (<c>EXTRA</c>); maximaler Schutz vor Datenverlust auf Kosten der Performance.
        /// </summary>
        Extra
    }
}
