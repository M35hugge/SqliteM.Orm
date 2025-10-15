using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace SQLiteM.Abstractions
{
    /// <summary>
    /// Erzeugt neue Datenbankverbindungen.
    /// </summary>
    /// <remarks>
    /// Die zurückgegebene Verbindung ist initial <c>geschlossen</c>. 
    /// Der Aufrufer ist für das Öffnen und Schließen verantwortlich.
    /// </remarks>
    public interface IConnectionFactory
    {
        /// <summary>
        /// Erstellt eine neue, noch nicht geöffnete <see cref="IDbConnection"/>.
        /// </summary>
        /// <returns>Eine neue, geschlossene <see cref="IDbConnection"/>.</returns>
        IDbConnection Create();
    }

    /// <summary>
    /// Bietet Dialekt-spezifische Operationen (Identifierquoting, Parameterpräfix).
    /// </summary>
    /// <remarks>
    /// Implementierungen legen fest, wie Bezeichner maskiert und Parameter benannt werden.
    /// </remarks>
    public interface ISqlDialect
    {
        /// <summary>
        /// Maskiert einen Bezeichner (z. B. Tabellen- oder Spaltennamen) für den Ziel-Dialekt.
        /// </summary>
        /// <param name="name">Ungequoteter Bezeichner.</param>
        /// <returns>Gequoteter Bezeichner.</returns>
        string QuoteIdentifier(string name);

        /// <summary>
        /// Präfix für SQL-Parameter (z. B. <c>@</c>).
        /// </summary>
        string ParameterPrefix { get; }
    }

    /// <summary>
    /// Liefert Mapping-Informationen für Entitätstypen.
    /// </summary>
    /// <remarks>
    /// Die Zuordnung basiert typischerweise auf Attributen (z. B. <c>[Table]</c>, <c>[Column]</c>, ...).
    /// </remarks>
    public interface IEntityMapper
    {
        /// <summary>
        /// Bestimmt den Tabellennamen für einen Entitätstyp.
        /// </summary>
        /// <param name="entityType">Der Entitätstyp.</param>
        /// <returns>Der Tabellenname in der Datenbank.</returns>
        string GetTableName(Type entityType);

        /// <summary>
        /// Gibt die Spaltenzuordnungen (Property ↔ Spalte) für den Entitätstyp zurück.
        /// </summary>
        /// <param name="entityType">Der Entitätstyp.</param>
        /// <returns>Schreibgeschützte Liste der Spaltenzuordnungen.</returns>
        IReadOnlyList<PropertyMap> GetPropertyMaps(Type entityType);

        /// <summary>
        /// Liefert die Primärschlüssel-Property, falls vorhanden.
        /// </summary>
        /// <param name="entityType">Der Entitätstyp.</param>
        /// <returns>Primärschlüssel-Mapping oder <see langword="null"/>.</returns>
        PropertyMap? GetPrimaryKey(Type entityType);

        /// <summary>
        /// Liefert definierte Fremdschlüssel des Entitätstyps.
        /// </summary>
        /// <param name="entityType">Der Entitätstyp.</param>
        /// <returns>Schreibgeschützte Liste der Fremdschlüsselzuordnungen.</returns>
        IReadOnlyList<ForeignKeyMap> GetForeignKeys(Type entityType);
    }

    /// <summary>
    /// Lesezugriff auf Entitäten.
    /// </summary>
    /// <typeparam name="T">Entitätstyp.</typeparam>
    public interface IReadRepository<T> where T : class, new()
    {
        /// <summary>
        /// Lädt alle Entitäten.
        /// </summary>
        /// <param name="ct">Abbruchtoken.</param>
        /// <returns>Schreibgeschützte Liste aller Entitäten.</returns>
        Task<IReadOnlyList<T>> FindAllAsync(CancellationToken ct = default);

        /// <summary>
        /// Führt eine einfache Abfrage mit WHERE (=) und ORDER BY aus.
        /// </summary>
        /// <param name="query">Query-Objekt mit Filter/Sortierung (Spaltennamen verwenden).</param>
        /// <param name="ct">Abbruchtoken.</param>
        /// <returns>Schreibgeschützte Liste der gefundenen Entitäten.</returns>
        Task<IReadOnlyList<T>> QueryAsync(Query query, CancellationToken ct = default);

        /// <summary>
        /// Findet eine Entität per Primärschlüssel.
        /// </summary>
        /// <param name="id">Primärschlüsselwert.</param>
        /// <param name="ct">Abbruchtoken.</param>
        /// <returns>Gefundene Entität oder <see langword="null"/>.</returns>
        Task<T?> FindByIdAsync(object id, CancellationToken ct = default);
    }

    /// <summary>
    /// Schreibzugriff auf Entitäten.
    /// </summary>
    /// <typeparam name="T">Entitätstyp.</typeparam>
    public interface IWriteRepository<T> where T : class, new()
    {
        /// <summary>
        /// Fügt eine Entität ein und gibt den (ggf. autogenerierten) Primärschlüssel zurück.
        /// </summary>
        /// <param name="entity">Einzufügende Entität.</param>
        /// <param name="ct">Abbruchtoken.</param>
        /// <returns>Primärschlüsselwert (z. B. <c>last_insert_rowid()</c>) oder <c>0</c>.</returns>
        Task<long> InsertAsync(T entity, CancellationToken ct = default);

        /// <summary>
        /// Aktualisiert eine bestehende Entität.
        /// </summary>
        /// <param name="entity">Zu aktualisierende Entität.</param>
        /// <param name="ct">Abbruchtoken.</param>
        /// <returns>Anzahl betroffener Zeilen.</returns>
        Task<int> UpdateAsync(T entity, CancellationToken ct = default);

        /// <summary>
        /// Löscht eine Entität per Primärschlüssel.
        /// </summary>
        /// <param name="id">Primärschlüsselwert.</param>
        /// <param name="ct">Abbruchtoken.</param>
        /// <returns>Anzahl betroffener Zeilen.</returns>
        Task<int> DeleteAsync(object id, CancellationToken ct = default);
    }

    /// <summary>
    /// Erzeugt SQL-Befehle für CRUD- und DDL-Operationen.
    /// </summary>
    public interface ISqlBuilder
    {
        /// <summary>
        /// Baut einen <c>INSERT</c>-Befehl für den Entitätstyp.
        /// </summary>
        /// <param name="entityType">Entitätstyp.</param>
        /// <param name="cols">Ausgabe: zu bindende Spalten (ohne Auto-Increment).</param>
        /// <returns>SQL-Befehl als String.</returns>
        string BuildInsert(Type entityType, out IReadOnlyList<PropertyMap> cols);

        /// <summary>
        /// Baut einen <c>UPDATE</c>-Befehl für den Entitätstyp.
        /// </summary>
        /// <param name="entityType">Entitätstyp.</param>
        /// <param name="cols">Ausgabe: zu setzende Spalten (ohne PK/Auto-Increment).</param>
        /// <returns>SQL-Befehl als String.</returns>
        string BuildUpdate(Type entityType, out IReadOnlyList<PropertyMap> cols);

        /// <summary>
        /// Baut einen <c>DELETE</c>-Befehl für den Entitätstyp.
        /// </summary>
        /// <param name="entityType">Entitätstyp.</param>
        /// <param name="key">Ausgabe: Primärschlüssel-Spalte.</param>
        /// <returns>SQL-Befehl als String.</returns>
        string BuildDelete(Type entityType, out PropertyMap key);

        /// <summary>
        /// Baut einen <c>SELECT</c>-Befehl, der per Primärschlüssel genau eine Zeile lädt.
        /// </summary>
        /// <param name="entityType">Entitätstyp.</param>
        /// <param name="key">Ausgabe: Primärschlüssel-Spalte.</param>
        /// <param name="cols">Ausgabe: alle zu lesenden Spalten.</param>
        /// <returns>SQL-Befehl als String.</returns>
        string BuildSelectById(Type entityType, out PropertyMap key, out IReadOnlyList<PropertyMap> cols);

        /// <summary>
        /// Baut eine <c>CREATE TABLE</c>-Anweisung inkl. Fremdschlüsseldefinitionen.
        /// </summary>
        /// <param name="entityType">Entitätstyp.</param>
        /// <returns>SQL-Befehl als String.</returns>
        string BuildCreateTable(Type entityType);
    }

    /// <summary>
    /// Fabrik für typisierte Repositories.
    /// </summary>
    public interface IRepositoryFactory
    {
        /// <summary>
        /// Erstellt ein Repository für den gegebenen Entitätstyp im angegebenen Unit-of-Work-Scope.
        /// </summary>
        /// <typeparam name="T">Entitätstyp.</typeparam>
        /// <param name="uow">Aktiver Unit-of-Work.</param>
        /// <returns>Eine neue Instanz von <see cref="IRepository{T}"/>.</returns>
        IRepository<T> Create<T>(IUnitOfWork uow) where T : class, new();
    }

    /// <summary>
    /// Kombiniert Lese- und Schreibfunktionen für Entitäten.
    /// </summary>
    /// <typeparam name="T">Entitätstyp.</typeparam>
    public interface IRepository<T> : IReadRepository<T>, IWriteRepository<T> where T : class, new() { }

    /// <summary>
    /// Fabrik zum Erstellen neuer Unit-of-Work-Sessions (Transaktionen).
    /// </summary>
    public interface IUnitOfWorkFactory
    {
        /// <summary>
        /// Erstellt einen neuen Unit-of-Work-Scope mit geöffneter Verbindung und gestarteter Transaktion.
        /// </summary>
        /// <param name="ct">Abbruchtoken.</param>
        /// <returns>Die erstellte <see cref="IUnitOfWork"/>.</returns>
        Task<IUnitOfWork> CreateAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// Einheitlicher Arbeits-/Transaktions-Scope: kapselt Verbindung und Transaktion.
    /// </summary>
    public interface IUnitOfWork : IAsyncDisposable
    {
        /// <summary>
        /// Zugehörige, geöffnete Datenbankverbindung.
        /// </summary>
        IDbConnection Connection { get; }

        /// <summary>
        /// Aktive Transaktion.
        /// </summary>
        IDbTransaction Transaction { get; }

        /// <summary>
        /// Bestätigt die Transaktion.
        /// </summary>
        /// <param name="ct">Abbruchtoken.</param>
        Task CommitAsync(CancellationToken ct = default);

        /// <summary>
        /// Verwirft die Transaktion.
        /// </summary>
        /// <param name="ct">Abbruchtoken.</param>
        Task RollbackAsync(CancellationToken ct = default);
    }

    // -------------------------
    // Helper-Records (Mapping)
    // -------------------------

    /// <summary>
    /// Beschreibt die Zuordnung einer CLR-Property zu einer DB-Spalte.
    /// </summary>
    /// <param name="ColumnName">Spaltenname in der Tabelle.</param>
    /// <param name="PropertyName">Name der CLR-Property.</param>
    /// <param name="PropertyType">CLR-Typ der Property.</param>
    /// <param name="IsPrimaryKey">Ob die Spalte Teil des Primärschlüssels ist.</param>
    /// <param name="IsAutoIncrement">Ob der Wert automatisch erhöht wird.</param>
    /// <param name="IsNullable">Ob NULL-Werte erlaubt sind.</param>
    /// <param name="Length">Maximale Länge (nur relevant für <c>string</c>).</param>
    public sealed record PropertyMap(
        string ColumnName,
        string PropertyName,
        Type PropertyType,
        bool IsPrimaryKey,
        bool IsAutoIncrement,
        bool IsNullable,
        int Length
    );

    /// <summary>
    /// Beschreibt eine Fremdschlüsselbeziehung.
    /// </summary>
    /// <param name="ThisColumn">Lokale Spalte (FK) in der aktuellen Tabelle.</param>
    /// <param name="PrincipalEntity">Typ der referenzierten Entität.</param>
    /// <param name="PrincipalTable">Tabellenname der referenzierten Entität.</param>
    /// <param name="PrincipalColumn">Spaltenname des referenzierten Primärschlüssels bzw. einer UNIQUE-Spalte.</param>
    /// <param name="OnDelete">Aktion bei Löschung der referenzierten Zeile.</param>
    public sealed record ForeignKeyMap(
        string ThisColumn,
        Type PrincipalEntity,
        string PrincipalTable,
        string PrincipalColumn,
        OnDeleteAction OnDelete
    );
}
