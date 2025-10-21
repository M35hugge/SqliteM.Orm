# SQLiteM – leichtgewichtiges ORM für SQLite

SQLiteM ist ein einfaches, attributbasiertes ORM für .NET-Anwendungen,  
das ein **Unit-of-Work**-Pattern mit einem **Repository-System** kombiniert – optimiert für **SQLite**.

---

## Schnellstart

```csharp
// Program.cs
using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using SQLiteM.Demo;
using SQLiteM.Orm;

string dbPath = Path.Combine(AppContext.BaseDirectory, "app.db");
string cs = $"Data Source={dbPath};Cache=Shared";

// DI aufsetzen
ServieProvider services = new ServiceCollection()
    .AddSQLiteM(o => o.ConnectionString = cs)
    .BuildServiceProvider();

// Schema einmal anlegen (vor CRUD)
SQLiteMClient client = new (cs);
Type[] entities = { typeof(Person), typeof(Order) };

await client.EnsureCreateAsync(new CancellationToken(), entities);

// Persons und Orders in die Datenbank schreiben
Person p = new Person { FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com" };
long id= await client.InsertAsync(p);

Order o1 = new Order { PersonId = id, Total = 19.99m, Note = "Notebook" };
Order o2 = new Order { PersonId = id, Total = 42.50m, Note = "Books" };
Order o3 = new Order { PersonId = id, Total = 5.00m, Note = "Coffee" };

await client.InsertAsync(o1);
await client.InsertAsync(o2);
await client.InsertAsync(o3);
Console.WriteLine($"Inserted Person {id} and 3 orders.");

// Orders einer Person laden (einfacher Query-Builder)
Order orders = await client.QueryAsync<Order>(Query.WhereEquals("person_id", id).OrderBy("id"));
foreach (Order o in orders)
    Console.WriteLine($"Order {o.Id}: total={o.Total} note={o.Note}");



// Cascade-Delete testen: Person löschen -> Orders werden mitgelöscht
await client.DeleteAsync<Person>(id);
Console.WriteLine($"Deleted person {id} (ON DELETE CASCADE should remove orders).");


// Verifizieren, dass keine Orders mehr existieren
Order remaining= await client.FindAllAsync<Order>();
Console.WriteLine($"Remaining orders: {remaining.Count}");


---

| Kategorie                                    | Beschreibung                                |
| -------------------------------------------- | ------------------------------------------- |
| [Getting Started](getting-started.md)        | Einführung, Installation und erste Schritte |
| [Mapping](mapping.md)                        | Attribute & Entitätskonfiguration           |
| [Unit of Work & Repository](unit-of-work.md) | Transaktionssteuerung und Datenzugriff      |
| [Queries](queries.md)                        | Einfache Filter- und Sortierabfragen        |
| [API](api/index.md)                          | Vollständige API-Referenz                   |



Warum SQLiteM?

Minimalistisch: Nur, was du wirklich brauchst.

Einfach konfigurierbar: Keine komplizierten Migrations-Tools.

Schnell & leicht: Ideal für Embedded- oder Desktop-Apps.

Vollständig async: Kompatibel mit modernen .NET-Patterns.




