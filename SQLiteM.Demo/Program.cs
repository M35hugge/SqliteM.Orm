using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using SQLiteM.Demo;
using SQLiteM.Orm.Pub;

var dbPath = Path.Combine(AppContext.BaseDirectory, "app.db");
var cs = $"Data Source={dbPath};Cache=Shared";

// DI aufsetzen
var services = new ServiceCollection()
    .AddSQLiteM(o => o.ConnectionString = cs, sp=>new SnakeCaseNameTranslator())
    .BuildServiceProvider();

// Schema einmal anlegen (vor CRUD)
SQLiteMClient client = new (cs);
Type[] entities = { typeof(Person), typeof(Order) };

await client.EnsureCreateAsync(new CancellationToken(), entities);

var p = new Person { FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com" };

int id= await client.InsertAsync(p);

Console.WriteLine("Id of Ada: " + id);

var o1 = new Order { PersonId = id, Total = 19.99m, Note = "Notebook" };
var o2 = new Order { PersonId = id, Total = 42.50m, Note = "Books" };
var o3 = new Order { PersonId = id, Total = 5.00m, Note = "Coffee" };

await client.InsertAsync(o1);
await client.InsertAsync(o2);
await client.InsertAsync(o3);
Console.WriteLine($"Inserted Person {id} and 3 orders.");

// Orders einer Person laden (einfacher Query-Builder)
var orders = await client.QueryAsync<Order>(Query.WhereEquals(nameof(Person.Id), id).OrderBy(nameof(Order.Id)));

foreach (var o in orders)
    Console.WriteLine($"Order {o.Id}: total={o.Total} note={o.Note}");

// Cascade-Delete testen: Person löschen -> Orders werden mitgelöscht
var id2 = await client.WithTransactionAsync(async tx =>
{
    var rp = tx.Repo<Person>();
    var ro = tx.Repo<Order>();
    var p = new Person { FirstName = "Grace", LastName = "Hopper" };
    var id = await rp.InsertAsync(p);
    await ro.InsertAsync(new Order { PersonId = id, Total = 10m });
    await ro.InsertAsync(new Order { PersonId = id, Total = 5m, Note = "Coffee" });

    return id;
});

Console.WriteLine("Id of Grace: " + id2);
var orders2 = await client.QueryAsync<Order>(Query.WhereEquals("person_id", id2).OrderBy(nameof(Order.Id)));
var orders3 = await client.QueryAsync<Order>(Query.WhereEquals("PersonId", id2).OrderBy(nameof(Order.Id)));

foreach (var o in orders2)
{
    var note = !string.IsNullOrEmpty(o.Note) ? o.Note : " - ";
    Console.WriteLine($"Order {o.Id}: total={o.Total} note={note}"); 
}
foreach (var o in orders3)
{
    var note = !string.IsNullOrEmpty(o.Note) ? o.Note : " - ";
    Console.WriteLine($"Order {o.Id}: total={o.Total} note={note}");
}
await client.DeleteAsync<Person>(id);
await client.DeleteAsync<Person>(id2);

Console.WriteLine($"Deleted person {id} (ON DELETE CASCADE should remove orders).");
Console.WriteLine($"Deleted person {id2} (ON DELETE CASCADE should remove orders).");

// Verifizieren, dass keine Orders mehr existieren
var remaining= await client.FindAllAsync<Order>();
Console.WriteLine($"Remaining orders: {remaining.Count}");
