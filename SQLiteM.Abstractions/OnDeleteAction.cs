namespace SQLiteM.Abstractions;

/// <summary>
/// Repräsentiert die möglichen <c>ON DELETE</c>-Aktionen für Fremdschlüssel
/// in SQLite. Diese Werte geben an, wie sich das Löschen eines Datensatzes
/// in der referenzierten Tabelle auf die referenzierenden Datensätze auswirkt.
/// </summary>
/// <remarks>
/// Die hier definierten Optionen entsprechen den in SQLite unterstützten
/// Aktionen gemäß der Syntax <c>FOREIGN KEY ... ON DELETE ...</c>.
/// Welche Aktion sinnvoll ist, hängt vom gewünschten Datenkonsistenz- und
/// Kaskadierungsverhalten des Modells ab.
/// </remarks>
/// <seealso href="https://www.sqlite.org/foreignkeys.html">SQLite-Dokumentation: Foreign Key Support</seealso>
public enum OnDeleteAction
{
    /// <summary>
    /// Es wird keine spezielle Aktion ausgeführt. Ein Löschversuch in der
    /// referenzierten Tabelle schlägt fehl, wenn referenzierende Zeilen existieren
    /// und die Fremdschlüsselprüfung aktiv ist.
    /// Entspricht SQLite: <c>ON DELETE NO ACTION</c>.
    /// </summary>
    NoAction,

    /// <summary>
    /// Verhindert das Löschen eines Datensatzes in der referenzierten Tabelle,
    /// solange abhängige (referenzierende) Zeilen existieren.
    /// Entspricht SQLite: <c>ON DELETE RESTRICT</c>.
    /// </summary>
    Restrict,

    /// <summary>
    /// Löscht automatisch alle referenzierenden Zeilen, wenn der zugehörige
    /// Datensatz in der referenzierten Tabelle gelöscht wird.
    /// Entspricht SQLite: <c>ON DELETE CASCADE</c>.
    /// </summary>
    Cascade,

    /// <summary>
    /// Setzt den Fremdschlüssel in allen referenzierenden Zeilen auf <c>NULL</c>,
    /// wenn der zugehörige Datensatz in der referenzierten Tabelle gelöscht wird.
    /// Entspricht SQLite: <c>ON DELETE SET NULL</c>.
    /// </summary>
    SetNull,

    /// <summary>
    /// Setzt den Fremdschlüssel in allen referenzierenden Zeilen auf den
    /// Spalten-Standardwert, wenn der zugehörige Datensatz in der
    /// referenzierten Tabelle gelöscht wird.
    /// Entspricht SQLite: <c>ON DELETE SET DEFAULT</c>.
    /// </summary>
    SetDefault
}
