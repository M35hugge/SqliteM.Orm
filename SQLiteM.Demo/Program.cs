using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using SQLiteM.Demo;
using SQLiteM.Orm;
using SQLiteM.Orm.Internal;

var dbPath = Path.Combine(AppContext.BaseDirectory, "app.db");
var cs = $"Data Source={dbPath};Cache=Shared";

// DI aufsetzen
var services = new ServiceCollection()
    .AddSQLiteM(o => o.ConnectionString = cs)
    .BuildServiceProvider();

// Schema einmal anlegen (vor CRUD)
await using (var uow = await services.GetRequiredService<IUnitOfWorkFactory>().CreateAsync())
{
    var sqlBuilder = services.GetRequiredService<ISqlBuilder>();
    await SQLiteMBootstrap.EnsureCreatedAsync<Person>(uow, sqlBuilder);
    await SQLiteMBootstrap.EnsureCreatedAsync<Order>(uow, sqlBuilder);
    await uow.CommitAsync();
}


long personId;
await using (var uow = await services.GetRequiredService<IUnitOfWorkFactory>().CreateAsync())
{
    var repoPerson = services.GetRequiredService<IRepositoryFactory>().Create<Person>(uow);
    var repoOrder  = services.GetRequiredService<IRepositoryFactory>().Create<Order>(uow);

    var p = new Person { FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com" };
    personId = await repoPerson.InsertAsync(p);

    await repoOrder.InsertAsync(new Order { PersonId = personId, Total = 19.99m, Note = "Notebook" });
    await repoOrder.InsertAsync(new Order { PersonId = personId, Total = 42.50m, Note = "Books" });
    await repoOrder.InsertAsync(new Order { PersonId = personId, Total = 5.00m,  Note = "Coffee" });

    await uow.CommitAsync();
    Console.WriteLine($"Inserted Person {personId} and 3 orders.");
}

// Orders einer Person laden (einfacher Query-Builder)
await using (var uow = await services.GetRequiredService<IUnitOfWorkFactory>().CreateAsync())
{
    var repoOrder = services.GetRequiredService<IRepositoryFactory>().Create<Order>(uow);

    var orders = await repoOrder.QueryAsync(
        Query.WhereEquals("person_id", personId).OrderBy("id"));

    foreach (var o in orders)
        Console.WriteLine($"Order {o.Id}: total={o.Total} note={o.Note}");

    await uow.CommitAsync();
}

// Cascade-Delete testen: Person löschen -> Orders werden mitgelöscht
await using (var uow = await services.GetRequiredService<IUnitOfWorkFactory>().CreateAsync())
{
    var repoPerson = services.GetRequiredService<IRepositoryFactory>().Create<Person>(uow);
    await repoPerson.DeleteAsync(personId);
    await uow.CommitAsync();
    Console.WriteLine($"Deleted person {personId} (ON DELETE CASCADE should remove orders).");
}

// Verifizieren, dass keine Orders mehr existieren
await using (var uow = await services.GetRequiredService<IUnitOfWorkFactory>().CreateAsync())
{
    var repoOrder = services.GetRequiredService<IRepositoryFactory>().Create<Order>(uow);
    var remaining = await repoOrder.FindAllAsync();
    Console.WriteLine($"Remaining orders: {remaining.Count}");
}