using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using SQLiteM.Demo;
using SQLiteM.Orm;

var dbPath = Path.Combine(AppContext.BaseDirectory, "app.db");
var cs = $"Data Source={dbPath};Cache=Shared";

// DI aufsetzen
var services = new ServiceCollection()
    .AddSQLiteM(o => o.ConnectionString = cs)
    .BuildServiceProvider();

// Schema einmal anlegen (vor CRUD)
SQLiteMClient client = new (cs);
Type[] entities = { typeof(Person), typeof(Order) };

await client.EnsureCreateAsync(new CancellationToken(), entities);

var p = new Person { FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com" };
long id= await client.InsertAsync(p);

var o1 = new Order { PersonId = id, Total = 19.99m, Note = "Notebook" };
var o2 = new Order { PersonId = id, Total = 42.50m, Note = "Books" };
var o3 = new Order { PersonId = id, Total = 5.00m, Note = "Coffee" };

await client.InsertAsync(o1);
await client.InsertAsync(o2);
await client.InsertAsync(o3);
Console.WriteLine($"Inserted Person {id} and 3 orders.");

// Orders einer Person laden (einfacher Query-Builder)
var orders = await client.QueryAsync<Order>(Query.WhereEquals("person_id", id).OrderBy("id"));
foreach (var o in orders)
    Console.WriteLine($"Order {o.Id}: total={o.Total} note={o.Note}");



// Cascade-Delete testen: Person löschen -> Orders werden mitgelöscht

await client.DeleteAsync<Person>(id);
Console.WriteLine($"Deleted person {id} (ON DELETE CASCADE should remove orders).");


// Verifizieren, dass keine Orders mehr existieren
var remaining= await client.FindAllAsync<Order>();
Console.WriteLine($"Remaining orders: {remaining.Count}");
