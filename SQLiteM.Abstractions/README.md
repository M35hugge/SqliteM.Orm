# SQLiteM.Abstractions

`SQLiteM.Abstractions` enthält die **öffentlichen Contracts** (Attribute, Interfaces, Records), auf denen das Micro-ORM `SQLiteM.Orm` basiert.

Dieses Paket:
- definiert, **wie** ein Entity gemappt wird,
- beschreibt die **Unit of Work**- und **Repository**-APIs,
- enthält alle nötigen Attribute (`[Table]`, `[Column]`, `[PrimaryKey]`, …),
- aber **keine Implementierung** (kein Sqlite-Zugriff, kein SQL-Building).

Wenn du die ORM-Implementierung möchtest:  
→ installiere zusätzlich `SQLiteM.Orm`.

```bash
dotnet add package SQLiteM.Abstractions
dotnet add package SQLiteM.Orm
```


---

## Kern-Konzepte

### 1. Mapping-Attribute

#### `[Table]`
Weist der Klasse eine Tabelle zu.

```csharp
[Table("persons")]
public class Person { ... }
```

Ohne Namen kann die Tabelle per Konvention benannt werden (z. B. über einen `INameTranslator` wie snake_case).

#### `[Column]`
Weist einem Property eine Spalte zu, inkl. Nullbarkeit usw.:

```csharp
[Column("first_name", IsNullable = false, Length = 100)]
public string FirstName { get; set; } = default!;
```

Wichtige Flags:
- `IsNullable` – ob `NULL` erlaubt ist
- `Length` – deklarierte Länge (z. B. `VARCHAR(100)`), informativ für SQLite
- `IsUniqueColumn` – erzeugt ein `UNIQUE`-Constraint direkt in der `CREATE TABLE`-Definition

Wenn du `[Column]` weglässt, werden Spaltennamen per Konvention erzeugt (z. B. `FirstName` → `first_name` mit SnakeCase-Translator).

#### `[PrimaryKey]`
Markiert den Primärschlüssel.

```csharp
[PrimaryKey]
public long Id { get; set; }
```

Kann oft entfallen: `Id` oder `<TypeName>Id` wird automatisch als Schlüssel erkannt.

#### `[AutoIncrement]`
Markiert eine PK-Spalte als Auto-Increment (SQLite `AUTOINCREMENT`).

```csharp
[PrimaryKey, AutoIncrement]
public long Id { get; set; }
```

Beim Insert ruft das ORM `last_insert_rowid()` ab und setzt die Property am Objekt.

#### `[ForeignKey]`
Definiert eine FK-Beziehung zwischen Tabellen.

```csharp
[ForeignKey(typeof(Person), nameof(Person.Id), OnDelete = OnDeleteAction.Cascade)]
public long PersonId { get; set; }
```

Unterstützte `OnDeleteAction`-Werte:
- `NoAction`
- `Restrict`
- `Cascade`
- `SetNull`
- `SetDefault`

Diese Information wird in die `CREATE TABLE`-DDL übernommen.

#### `[Index]`
Erzeugt (falls nötig) einen Index über eine einzelne Spalte.

```csharp
[Index] // nicht-unique
public string LastName { get; set; } = default!;

[Index(IsUnique = true)]
public string Email { get; set; } = default!;
```

Das ORM generiert dann `CREATE INDEX IF NOT EXISTS ...` (oder `CREATE UNIQUE INDEX IF NOT EXISTS ...`) beim Schema-Bootstrap.

#### `[CompositeIndex]`
Definiert einen (optionalen unique) Mehrspaltenindex auf Klassenebene.

```csharp
[CompositeIndex(nameof(LastName), nameof(FirstName), IsUnique = false)]
public class Person { ... }
```

Das ORM löst die CLR-Propertynamen in DB-Spalten auf und erzeugt z. B.:

```sql
CREATE INDEX IF NOT EXISTS ix_person_last_name_first_name
ON person(last_name, first_name);
```

#### `[Ignore]`
Property wird NICHT gemappt (z. B. Navigations-Property oder berechneter Wert).

```csharp
[Ignore]
public List<Order> Orders { get; } = new();
```


---

## 2. Records / Metadatenstrukturen

Diese Records beschreiben, wie dein Modell letztlich in SQL übersetzt wird (wird intern vom ORM verwendet, aber ist öffentlich dokumentiert):

### `PropertyMap`
- `ColumnName` (DB-Name)
- `PropertyName` (CLR-Name)
- `PropertyType`
- `IsPrimaryKey`
- `IsAutoIncrement`
- `IsNullable`
- `IsUniqueColumn`
- `IsIndex`
- `IsUniqueIndex`
- `Length`

### `ForeignKeyMap`
- `ThisColumn` (FK-Spalte in dieser Tabelle)
- `PrincipalEntity` (CLR-Typ der referenzierten Tabelle)
- `PrincipalTable`
- `PrincipalColumn`
- `OnDelete`

### `IndexMap`
- `Name`
- `Columns` (eine oder mehrere Spalten)
- `IsUnique`


---

## 3. Repository- und Transaktions-APIs

Diese Interfaces definieren die öffentliche Oberfläche, die `SQLiteM.Orm` dann implementiert.

### `IUnitOfWork`
Kapselt Connection + laufende Transaktion.
- `CommitAsync()`
- `RollbackAsync()`
- `Connection`
- `Transaction`

Lebensdauer: **pro Scope / pro Transaktion**.

### `IUnitOfWorkFactory`
Erzeugt eine neue `IUnitOfWork`.

Typischer Ablauf:
```csharp
await using var uow = await uowFactory.CreateAsync();
...
await uow.CommitAsync();
```

### `IRepository<T>`
CRUD + einfache Abfragen:
- `InsertAsync(entity)`
- `UpdateAsync(entity)`
- `DeleteAsync(id)`
- `FindByIdAsync(id)`
- `FindAllAsync()`
- `QueryAsync(Query q)`

### `IRepositoryFactory`
Erstellt `IRepository<T>` für einen bestimmten `IUnitOfWork`.

```csharp
var repo = repoFactory.Create<Person>(uow);
```

Alle Operationen laufen damit in derselben Transaktion.

### `ITransactionContext`
Wird in der High-Level API (`SQLiteMClient.WithTransactionAsync(...)`) benutzt.
- `Repo<T>()`
- `CommitAsync()`
- `RollbackAsync()`
- `IsCompleted`
- `Uow`

Das ist im Prinzip ein "scoped UnitOfWork + typed repos"-Wrapper.

### `Query`
Ultraleichtes Query-Objekt für `WHERE col = value` + `ORDER BY col`.

```csharp
var q = Query
    .WhereEquals("last_name", "Hopper")
    .OrderBy("first_name", desc: false);

var rows = await repo.QueryAsync(q);
```

Die Spaltennamen sind i. d. R. DB-Spaltennamen (z. B. `first_name`).  
Das ORM versucht, sinnvolle Übersetzung zwischen CLR (`FirstName`) und DB (`first_name`) zu machen – abhängig vom konfigurierten `INameTranslator`.


---

## 4. Name Translation (Konventionen statt Boilerplate)

Über `INameTranslator` kannst du festlegen, wie Klassen-/Propertynamen zu Tabellen-/Spaltennamen werden, falls kein `[Table]/[Column]` angegeben ist.

Das Interface:
```csharp
public interface INameTranslator
{
    string Table(string clrTypeName);
    string Column(string clrPropertyName);
    string Property(string fieldName); // reverse mapping
}
```

Beispiel: `SnakeCaseNameTranslator`
- `PersonOrder` → `person_order`
- `FirstName`   → `first_name`
- `person_id`   → `PersonId` (für Rückabbildung in die CLR-Property)


---

## 5. Warum ein eigenes Abstractions-Paket?

- Du kannst eigene Implementierungen schreiben (andere Dialekte, andere ConnectionFactories, Mock-Repos für Tests, etc.), ohne `SQLiteM.Orm` referenzieren zu müssen.
- Du kannst in deiner Applikation oder in Tests nur die Contracts sharen.
- Strikte Trennung zwischen "API/Contracts" (dieses Paket) und "Default SQLite-Implementation" (`SQLiteM.Orm`).


---

## 6. Nächster Schritt

Wenn du nur Models annotieren willst → **dieses Paket reicht**.  
Wenn du wirklich mit SQLite reden willst:

```bash
dotnet add package SQLiteM.Orm
```

Dann kannst du:

- Tabellen erstellen (`SQLiteMBootstrap.EnsureCreatedAsync`)
- Repositories über DI nutzen
- oder mit `new SQLiteMClient("Data Source=whatever.db")` alles ohne DI fahren

Und das alles mit Transaktionen, Foreign Keys, Indexen, Unique Constraints und predictable SQL.
