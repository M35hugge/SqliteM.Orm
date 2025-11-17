# SQLiteM – leichtgewichtiges ORM für SQLite

SQLiteM ist ein einfaches, attributbasiertes ORM für .NET-Anwendungen,  
das ein **Unit-of-Work**-Pattern mit einem **Repository-System** kombiniert für **SQLite**.

---

## Schnellstart

```csharp
// Program.cs
using SQLiteM.Abstractions;
using SQLiteM.Orm.Pub;

var dbPath = Path.Combine(AppContext.BaseDirectory, "app.db");
var cs = $"Data Source={dbPath};Cache=Shared";

// High-Level API: SQLiteMClient
await using var client = new SQLiteMClient(cs);

// Schema einmalig anlegen (vor CRUD)
// Person und Order sind deine Entity-Typen (siehe Mapping-Doku)
await client.EnsureCreatedAsync(typeof(Person), typeof(Order));

// Person und Orders in die Datenbank schreiben
var person = new Person
{
    FirstName = "Ada",
    LastName  = "Lovelace",
    Email     = "ada@example.com"
};

var id = await client.InsertAsync(person);

var o1 = new Order { PersonId = id, Total = 19.99m, Note = "Notebook" };
var o2 = new Order { PersonId = id, Total = 42.50m, Note = "Books" };
var o3 = new Order { PersonId = id, Total = 5.00m, Note = "Coffee" };

await client.InsertAsync(o1);
await client.InsertAsync(o2);
await client.InsertAsync(o3);

Console.WriteLine($"Inserted person {id} and 3 orders.");

// Orders einer Person laden (einfacher Query-Builder)
var orders = await client.QueryAsync<Order>(
    Query.WhereEquals("person_id", id).OrderBy("id"));

foreach (var o in orders)
{
    Console.WriteLine($"Order {o.Id}: total={o.Total} note={o.Note}");
}

// Cascade-Delete testen: Person löschen -> Orders werden mitgelöscht
await client.DeleteAsync<Person>(id);
Console.WriteLine($"Deleted person {id} (ON DELETE CASCADE should remove orders).");

// Verifizieren, dass keine Orders mehr existieren
var remaining = await client.FindAllAsync<Order>();
Console.WriteLine($"Remaining orders: {remaining.Count}");
```

---

| Kategorie                                             | Beschreibung                                |
| ----------------------------------------------------- | ------------------------------------------- |
| [Getting Started](getting-started.md)                 | Einführung, Installation und erste Schritte |
| [Mapping](mapping.md)                                 | Attribute & Entitätskonfiguration           |
| [Unit of Work & Repository](unit-of-work-repository.md) | Transaktionssteuerung und Datenzugriff      |
| [Queries](queries.md)                                 | Einfache Filter- und Sortierabfragen        |
| [API](api/index.md)                                   | Vollständige API-Referenz                   |


Warum SQLiteM?

Minimalistisch: Nur, was du wirklich brauchst.

Einfach konfigurierbar: Keine komplizierten Migrations-Tools.

Schnell & leicht: Ideal für Embedded- oder Desktop-Apps.

Vollständig async: Kompatibel mit modernen .NET-Patterns.
