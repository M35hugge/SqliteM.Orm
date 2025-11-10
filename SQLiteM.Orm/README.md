# SQLiteM.Orm

`SQLiteM.Orm` ist ein leichtgewichtiges Micro-ORM für SQLite (.NET), das bewusst simpel bleibt:
- Keine Magie zur Laufzeit (kein Expression Tree SQL-Generator)
- Fokus auf prediktables SQL und volle Kontrolle
- Attribute & Konventionen statt komplexer Migrations-APIs

Du bekommst:
- **Unit of Work + Transaktion**  
- **Repositories (CRUD + einfache Queries)**  
- **[ForeignKey(... OnDelete = Cascade)] inklusive PRAGMA foreign_keys=ON**  
- **Automatische Namensübersetzung (z. B. CLR `FirstName` → DB `first_name`)**  
- **`SQLiteMClient` high level API (Zero-DI)**  
- **Index-Attribute und Unique Constraints**  
- **`EnsureCreatedAsync` statt schwergewichtiger Migrationen**


---

## Installation

```bash
dotnet add package SQLiteM.Orm
```

Wichtig:
- `SQLiteM.Orm` hängt von `SQLiteM.Abstractions` ab.  
Das wird automatisch mit installiert.


---

## 1. Setup per Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Orm.Pub; // AddSQLiteM extension + SQLiteMBootstrap
using SQLiteM.Abstractions;

var dbPath = Path.Combine(AppContext.BaseDirectory, "app.db");

// klassische SQLite-Connection-String
var cs = $"Data Source={dbPath};Cache=Shared";

var services = new ServiceCollection()
    .AddSQLiteM(opt => opt.ConnectionString = cs)
    .BuildServiceProvider();
```

Was `AddSQLiteM(...)` für dich registriert:

- `IConnectionFactory` → SQLite Connection
- `IUnitOfWorkFactory` → öffnet Connection, startet Transaktion, setzt `PRAGMA foreign_keys = ON`
- `IRepositoryFactory` → erstellt Repositories pro Transaktion
- `IEntityMapper` → liest Attribute / Konventionen (Table, Column, PK, FK, Index, etc.)
- `ISqlBuilder` → generiert SQL (`INSERT`, `UPDATE`, `CREATE TABLE`, `CREATE INDEX`, …)
- `ISqlDialect` → kümmert sich um Quoting/Parameterpräfix (`"table"`, `@param`)
- `INameTranslator` → z. B. snake_case


---

## 2. Entity definieren

### Variante A: Voll explizit mit Attributen

```csharp
using SQLiteM.Abstractions;

[Table("persons")]
public sealed class Person
{
    [PrimaryKey, AutoIncrement]
    [Column("id", IsNullable = false)]
    public long Id { get; set; }

    [Column("first_name", Length = 100, IsNullable = false)]
    public string FirstName { get; set; } = default!;

    [Column("last_name", Length = 100, IsNullable = false)]
    public string LastName { get; set; } = default!;

    [Column("email", Length = 255, IsNullable = true, IsUniqueColumn = true)]
    public string? Email { get; set; }
}
```

### Variante B: Fast ohne Attribute (Convention-based)

Wenn du `SnakeCaseNameTranslator` verwendest, kannst du Attribute weglassen.  
Beispiel:

```csharp
public sealed class Person
{
    // Wird automatisch als PK erkannt durch Name "Id"
    // -> Spalte "id" (snake_case)
    public long Id { get; set; }

    // Property "FirstName" wird zu Spalte "first_name"
    public string FirstName { get; set; } = default!;

    public string LastName  { get; set; } = default!;

    public string? Email    { get; set; }
}
```

- `[PrimaryKey]` ist optional, wenn das Property `Id` oder `<TypeName>Id` heißt.
- Spaltennamen kommen vom `INameTranslator` (standardmäßig `IdentityNameTranslator`, optional Snake Case).
- `[Ignore]` kannst du benutzen, um Properties aus der DB auszuschließen (z. B. Navigationen).


---

## 3. Tabellen erzeugen (Schema Bootstrap)

Bevor du Inserts machst, muss die Tabelle existieren.  
Das erledigt `EnsureCreatedAsync`. Es erzeugt:
- `CREATE TABLE IF NOT EXISTS ...`
- `FOREIGN KEY (...) REFERENCES (...) ON DELETE ...`
- alle definierten Indizes (`[Index]`, `[CompositeIndex]`)
- `UNIQUE` Constraints

```csharp
await using (var uow = await services.GetRequiredService<IUnitOfWorkFactory>().CreateAsync())
{
    var sqlBuilder = services.GetRequiredService<ISqlBuilder>();

    // einzelne Tabelle
    await SQLiteMBootstrap.EnsureCreatedAsync<Person>(uow, sqlBuilder);

    // mehrere Tabellen in kontrollierter Reihenfolge (wichtig bei FKs!)
    await SQLiteMBootstrap.EnsureCreatedAsync(
        uow,
        sqlBuilder,
        default,
        typeof(Person),
        typeof(Order));

    await uow.CommitAsync();
}
```


---

## 4. Arbeiten mit UnitOfWork + Repository

```csharp
await using (var uow = await services.GetRequiredService<IUnitOfWorkFactory>().CreateAsync())
{
    var repoFactory = services.GetRequiredService<IRepositoryFactory>();
    var people = repoFactory.Create<Person>(uow);

    // INSERT
    var id = await people.InsertAsync(new Person {
        FirstName = "Ada",
        LastName  = "Lovelace",
        Email     = "ada@example.com"
    });

    // UPDATE
    var p = await people.FindByIdAsync(id);
    p!.Email = "ada.lovelace@history.org";
    await people.UpdateAsync(p);

    // QUERY
    var lovelaces = await people.QueryAsync(
        Query
            .WhereEquals("last_name", "Lovelace")  // Spaltenname, nicht CLR!
            .OrderBy("first_name")
    );

    // DELETE
    await people.DeleteAsync(id);

    await uow.CommitAsync();
}
```

Wichtig:
- Alles läuft in einer Transaktion.  
- `UnitOfWorkFactory.CreateAsync()` öffnet die Connection, setzt `PRAGMA foreign_keys = ON` und beginnt eine Transaktion.
- Nach `CommitAsync()` ist die UoW “verbraucht” – bitte nicht weiterverwenden.


---

## 5. High-Level API: SQLiteMClient

Wenn du kein DI willst, gibt’s die bequeme Fassade **`SQLiteMClient`**:
- kümmert sich um ServiceProvider intern
- öffnet/committet Transaktionen für dich
- sehr praktisch für Scripte, Tools, Tests, Konsolenapps

```csharp
using SQLiteM.Orm.Pub;

// Pfad oder fertiger Connection String möglich
await using var client = new SQLiteMClient("Data Source=mydb.db;Cache=Shared");

// Tabellen anlegen
await client.EnsureCreatedAsync<Person>();

// Insert
var id = await client.InsertAsync(new Person {
    FirstName = "Grace",
    LastName  = "Hopper"
});

// Transaktion mit mehreren Repositories
await client.WithTransactionAsync(async tx =>
{
    var people = tx.Repo<Person>();

    var p = await people.FindByIdAsync(id);
    p!.Email = "grace@example.com";

    await people.UpdateAsync(p);

    // du kannst weitere Repos holen, z. B. tx.Repo<Order>()

    await tx.CommitAsync();
});
```

Zusätzlich gibt's auch:
- `FindByIdAsync<T>(id)`
- `FindAllAsync<T>()`
- `QueryAsync<T>(Query q)`
- `DeleteAsync<T>(id)`
- `UpdateAsync<T>(entity)`
- `EnsureCreatedAsync(params Type[])`


---

## 6. Fremdschlüssel & Cascade Delete

```csharp
[Table("orders")]
public sealed class Order
{
    [PrimaryKey, AutoIncrement]
    public long Id { get; set; }

    [ForeignKey(typeof(Person), nameof(Person.Id), OnDelete = OnDeleteAction.Cascade)]
    public long PersonId { get; set; }

    public decimal Total { get; set; }

    public string? Note { get; set; }
}
```

Schema-Erzeugung in der richtigen Reihenfolge:

```csharp
await SQLiteMBootstrap.EnsureCreatedAsync(
    uow,
    sqlBuilder,
    default,
    typeof(Person), // principal
    typeof(Order)); // dependent
```

Wenn du dann eine `Person` löschst, löscht SQLite automatisch deren Orders (`ON DELETE CASCADE`), weil:
- `UnitOfWork` setzt immer `PRAGMA foreign_keys = ON;`
- `SqlBuilder` generiert die `FOREIGN KEY (...) REFERENCES ... ON DELETE CASCADE`-Klausel


---

## 7. Indizes & Unique

### Einfacher Index auf einer Spalte:
```csharp
[Index] // nicht-unique
public string LastName { get; set; } = default!;
```

Unique-Index:
```csharp
[Index(IsUnique = true)]
public string Email { get; set; } = default!;
```

Unique-Constraint auf Spaltenebene (also in CREATE TABLE, nicht separater Index):
```csharp
[Column(IsUniqueColumn = true)]
public string Email { get; set; } = default!;
```

Composite Index über mehrere Spalten:
```csharp
[CompositeIndex(nameof(LastName), nameof(FirstName), IsUnique = false)]
[Table("persons")]
public sealed class Person { ... }
```

Beim Schema-Bootstrap werden automatisch erzeugt:
- `CREATE TABLE IF NOT EXISTS ...`
- `CREATE INDEX IF NOT EXISTS ix_persons_last_name ON persons(last_name);`
- `CREATE UNIQUE INDEX IF NOT EXISTS ix_persons_email ON persons(email);`
- `CREATE INDEX IF NOT EXISTS ix_persons_last_name_first_name ON persons(last_name, first_name);`
- plus `UNIQUE` für Spalten mit `IsUniqueColumn=true`


---

## 8. Abfragen

`Query` ist ein kleiner Helper für einfache `WHERE col = value` + `ORDER BY`.  
(Absichtlich simpel, damit SQL nachvollziehbar bleibt.)

```csharp
var result = await repo.QueryAsync(
    Query
        .WhereEquals("last_name", "Hopper")  // Achtung: Spaltenname!
        .OrderBy("first_name", desc: false)
);
```

Hinweise:
- Die Column-Bezeichner, die du an `WhereEquals` oder `OrderBy` übergibst, sollten DB-Spaltennamen sein.
- Falls du snake_case verwendest und aus Versehen `FirstName` angibst, hilft der intern benutzte NameTranslator dabei, es trotzdem aufzulösen (Best-Effort).


---

## 9. Lebenszyklus / Philosophie

- Kein automatisches Lazy Loading.
- Kein riesiges Change Tracking.
- Keine globalen statischen Singletons.
- Du kannst Transaktionen vollständig kontrollieren (per `IUnitOfWorkFactory` oder `SQLiteMClient.WithTransactionAsync`).
- Das Mapping ist transparent (Records `PropertyMap`, `ForeignKeyMap`, `IndexMap` sind offen dokumentiert).


---

## 10. Typische Fehlerquellen

**"no such table"**  
→ Du hast `EnsureCreatedAsync` nicht aufgerufen oder nicht `CommitAsync()` nach dem Erstellen des Schemas gemacht.

**"FOREIGN KEY constraint failed"**  
→ `OnDeleteAction.Restrict` oder `NoAction` verhindert Löschen; oder du hast `PRAGMA foreign_keys` nicht aktiv. Das macht `UnitOfWork` für dich, aber nur innerhalb einer echten UoW, nicht in einer random Connection, die du manuell aufmachst.

**"Unknown column 'FirstName'" in Query**  
→ `Query.WhereEquals()` nimmt DB-Spaltennamen. Wenn du snake_case verwendest, heißt die Spalte vermutlich `first_name`.  
Der Resolver versucht beides zu akzeptieren (CLR oder DB-Name), aber bei exotischen Übersetzern kann’s knallen.


---

## 11. Wann sollte ich `SQLiteM.Orm` verwenden?

- Wenn du komplexe LINQ-Queries, Include/ThenInclude, automatische Joins und Migrationssystem wie EF Core brauchst.
- Wenn du DBs außer SQLite bedienen willst (aktueller Fokus ist SQLite).
- Wenn du (noch) keine volle Kontrolle über Transaktionen willst.

Wenn du aber:
- embedded SQLite fährst.
- konsistente Transaktionen willst.
- predictable SQL magst.
- und ≤1ms Overhead pro Query okay ist.


