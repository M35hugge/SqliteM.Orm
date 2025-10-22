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
    /// Der Aufrufer ist für das Öffnen (<see cref="IDbConnection.Open"/>) und Schließen
    /// (<see cref="IDbConnection.Close"/>) verantwortlich.
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
    /// Bietet Dialekt-spezifische Operationen (Identifier-Quoting, Parameterpräfix).
    /// </summary>
    /// <remarks>
    /// Implementierungen legen fest, wie Bezeichner maskiert und Parameter benannt werden
    /// (z. B. SQLite: <c>"name"</c>, <c>@param</c>).
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
    /// Die Zuordnung basiert typischerweise auf Attributen (z. B. <c>[Table]</c>, <c>[Column]</c>, …)
    /// und optional auf Konventionen (z. B. Namensübersetzung per <see cref="INameTranslator"/>).
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


        /// <summary>
        /// Liefert alle Indexdefinitionen (Einzel- und zusammengesetzte Indizes) zur Entität.
        /// </summary>
        /// <param name="entityType">Der Entitätstyp.</param>
        /// <returns>Schreibgeschützte Liste der Indexzuordnungen.</returns>
        IReadOnlyList<IndexMap> GetIndexes(Type entityType);
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
        Task<int> InsertAsync(T entity, CancellationToken ct = default);

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


        /// <summary>
        /// Baut <c>CREATE INDEX</c>-Anweisungen (Einzel- und Composite-Indizes).
        /// </summary>
        /// <param name="entityType">Entitätstyp.</param>
        /// <returns>Liste mit SQL-Texten (je Index ein Eintrag).</returns>
        IReadOnlyList<string> BuildCreateIndexes(Type entityType);
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

    /// <summary>
    /// Kontextobjekt für eine laufende Transaktion (Unit of Work).
    /// </summary>
    /// <remarks>
    /// Ein <see cref="ITransactionContext"/> kapselt eine geöffnete Verbindung samt aktiver
    /// Transaktion (<see cref="Uow"/>) und stellt typsichere Repositories bereit
    /// (<see cref="Repo{T}()"/>). Der Kontext ist für einen Scope gedacht:
    /// - Änderungen werden mit <see cref="CommitAsync"/> bestätigt oder via <see cref="RollbackAsync"/> verworfen.
    /// - Nach Abschluss (<see cref="IsCompleted"/> = <c>true</c>) sowie nach <see cref="IAsyncDisposable.DisposeAsync"/>
    ///   darf der Kontext nicht weiterverwendet werden.
    /// </remarks>
    public interface ITransactionContext : IAsyncDisposable
    {
        /// <summary>
        /// Liefert die zugrunde liegende <see cref="IUnitOfWork"/> (offene Verbindung + aktive Transaktion).
        /// </summary>
        /// <remarks>
        /// Über diese Arbeitseinheit werden alle Kommandos ausgeführt. Die Lebensdauer wird
        /// vom <see cref="ITransactionContext"/> verwaltet; die <see cref="IUnitOfWork"/> sollte
        /// außerhalb des Kontexts nicht separat disposed werden.
        /// </remarks>
        IUnitOfWork Uow { get; }

        /// <summary>
        /// Gibt ein typisiertes Repository für den angegebenen Entitätstyp zurück.
        /// </summary>
        /// <typeparam name="T">Der Entitätstyp, der durch das Repository verwaltet wird.</typeparam>
        /// <returns>
        /// Eine <see cref="IRepository{T}"/>-Instanz, die an die aktuelle <see cref="Uow"/> gebunden ist.
        /// </returns>
        /// <remarks>
        /// Das zurückgegebene Repository arbeitet innerhalb derselben Transaktion.
        /// Es ist nicht threadsicher; pro parallelem Vorgang sollte ein eigener Kontext verwendet werden.
        /// </remarks>
        IRepository<T> Repo<T>() where T : class, new();

        /// <summary>
        /// Bestätigt alle innerhalb dieses Kontexts vorgenommenen Änderungen.
        /// </summary>
        /// <param name="ct">Ein optionales <see cref="CancellationToken"/> zur Abbruchsteuerung.</param>
        /// <returns>Eine Aufgabe, die den Abschluss des Commit-Vorgangs signalisiert.</returns>
        /// <remarks>
        /// Nach erfolgreichem Commit gilt der Kontext als abgeschlossen (<see cref="IsCompleted"/> = <c>true</c>).
        /// Weitere Datenbankoperationen mit diesem Kontext sind nicht zulässig.
        /// </remarks>
        Task CommitAsync(CancellationToken ct = default);
        /// <summary>
        /// Bricht die Transaktion ab und verwirft alle nicht bestätigten Änderungen.
        /// </summary>
        /// <param name="ct">Ein optionales <see cref="CancellationToken"/> zur Abbruchsteuerung.</param>
        /// <returns>Eine Aufgabe, die den Abschluss des Rollback-Vorgangs signalisiert.</returns>
        /// <remarks>
        /// Nach einem Rollback gilt der Kontext als abgeschlossen (<see cref="IsCompleted"/> = <c>true</c>).
        /// </remarks>
        Task RollbackAsync(CancellationToken ct = default);
        /// <summary>
        /// Zeigt an, ob der Kontext abgeschlossen wurde (durch <see cref="CommitAsync"/> oder <see cref="RollbackAsync"/>).
        /// </summary>
        /// <value>
        /// <c>true</c>, wenn der Kontext bereits bestätigt oder zurückgerollt wurde; andernfalls <c>false</c>.
        /// </value>
        bool IsCompleted { get; }
    }

    /// <summary>
    /// Übersetzungsstrategie für Tabellen- und Spaltennamen.
    /// </summary>
    /// <remarks>
    /// Implementierungen können Namenskonventionen wie snake_case, SCREAMING_SNAKE
    /// oder CamelCase erzwingen. Der <see cref="IEntityMapper"/> ruft diese
    /// Übersetzer auf, um vom CLR-Namen (Klasse/Property) zu einem Datenbanknamen
    /// zu gelangen, sofern kein expliziter Name per Attribut angegeben ist.
    /// </remarks>
    public interface INameTranslator
    {
        /// <summary>
        /// Übersetzt den CLR-Typnamen zu einem Tabellennamen.
        /// </summary>
        /// <param name="clrTypeName">Der CLR-Name des Entitätstyps (z. B. "PersonOrder").</param>
        /// <returns>Der zu verwendende Tabellenname (z. B. "person_order").</returns>
        string Table(string clrTypeName);

        /// <summary>
        /// Übersetzt den CLR-Propertynamen zu einem Spaltennamen.
        /// </summary>
        /// <param name="clrPropertyName">Der CLR-Name des Properties (z. B. "PersonId").</param>
        /// <returns>Der zu verwendende Spaltenname (z. B. "person_id").</returns>
        string Column(string clrPropertyName);

        /// <summary>
        /// Übersetzt einen DB-Spaltennamen zurück zu einem CLR-Propertynamen.
        /// </summary>
        /// <param name="fieldName">DB-Spaltenname (z. B. <c>person_id</c>).</param>
        /// <returns>CLR-Propertyname (z. B. <c>PersonId</c>).</returns>
        string Property(string fieldName);
    }

}
