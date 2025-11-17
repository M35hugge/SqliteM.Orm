# Getting Started

## 1. Verbindung & Client

```csharp
using SQLiteM.Orm.Pub;

var dbPath = Path.Combine(AppContext.BaseDirectory, "app.db");
var cs = $"Data Source={dbPath};Cache=Shared";

// High-Level Client (Zero-DI)
await using var client = new SQLiteMClient(cs);
```

> Hinweis: Wenn du lieber DI + `IUnitOfWorkFactory`/`IRepositoryFactory` nutzen möchtest,  
> schau dir die Beispiele im SQLiteM.Orm-README und in der Unit-of-Work-Doku an.

---

## 2. Entity definieren

```csharp
using SQLiteM.Abstractions;

[Table("persons")]
public sealed class Person
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public long Id { get; set; }

    [Column("first_name", IsNullable = false, Length = 100)]
    public string FirstName { get; set; } = default!;

    [Column("last_name", IsNullable = false, Length = 100)]
    public string LastName { get; set; } = default!;

    [Column("email", IsNullable = true, Length = 255)]
    public string? Email { get; set; }
}
```

---

## 3. Schema erzeugen

```csharp
// erzeugt CREATE TABLE/INDEX IF NOT EXISTS usw.
await client.EnsureCreatedAsync<Person>();
```

Bei mehreren Entitäten kannst du z. B. schreiben:

```csharp
await client.EnsureCreatedAsync(typeof(Person), typeof(Order));
```

---

## 4. CRUD

```csharp
// Insert
var id = await client.InsertAsync(new Person
{
    FirstName = "Ada",
    LastName  = "Lovelace"
});

// GetById
var person = await client.FindByIdAsync<Person>(id);

// Update
if (person is not null)
{
    person.FirstName = "Ada Augusta";
    await client.UpdateAsync(person);
}

// Delete
await client.DeleteAsync<Person>(id);
```

Das ist der einfachste Einstieg: keine eigene Connection-Verwaltung, keine DI-Pflicht,  
aber trotzdem Transaktionen und Foreign-Key-Unterstützung unter der Haube.
