using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using SQLiteM.Demo;
using SQLiteM.Orm;

var cs = "Data Source=sql.db;Cache=Shared";

// Composition Root
var factory = new SqliteConnectionFactory(cs);
var dialect = new SqliteDialect();
var mapper = new ReflectionEntityMapper();
var builder = new SqlBuilder(mapper, dialect);

var services = new ServiceCollection()
    .AddSQLiteM(o => o.ConnectionString = "Data Source=customer.db;Cache=Shared")
    .BuildServiceProvider();

// Schema anlegen
await SQLiteMBootstrap.EnsureCreatedAsync<Person>(
    services.GetRequiredService<IUnitOfWorkFactory>(),
    services.GetRequiredService<ISqlBuilder>());

await using (var uow = new UnitOfWork(factory))
{
    await SchemaBootstrapper.EnsureCreatedAsync<Person>(uow, builder);

    var repo = new Repository<Person>(uow, mapper, builder, dialect);

    var p = new Person
    {
        FirstName = "Ada",
        LastName = "Lovelace",
        Email = "ada@example.com"
    };

    var id = await repo.InsertAsync(p);
    await uow.CommitAsync();

    WriteLine($"Inserted Id: {id}");
}

await using (var uow = new UnitOfWork(factory))
{
    var repo = new Repository<Person>(uow, mapper, builder, dialect);

    var found = await repo.FindByIdAsync(1);
    Console.WriteLine(found is null
        ? "Not found"
        : $"{found.Id}: {found.FirstName} {found.LastName} ({found.Email})");

    if (found is not null)
    {
        found.Email = "ada.lovelace@history.org";
        await repo.UpdateAsync(found);
        await uow.CommitAsync();
        WriteLine("Updated email.");
    }
}

await using (var uow = new UnitOfWork(factory))
{
    var repo = new Repository<Person>(uow, mapper, builder, dialect);
    await repo.DeleteAsync(1);
    await uow.CommitAsync();
    WriteLine("Deleted id 1.");
}

static void WriteLine(string message)
{
    Console.WriteLine(message);
}