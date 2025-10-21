# Getting Started

## 1. Services registrieren
```csharp
using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using SQLiteM.Orm;

var dbPath = Path.Combine(AppContext.BaseDirectory, "app.db");
var cs = $"Data Source={dbPath};Cache=Shared";
var services = new ServiceCollection()
    .AddSQLiteM(o => o.ConnectionString = cs)
    .BuildServiceProvider();
```
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
## 3. Schema erzeugen
```csharp
await using (var uow = await services.GetRequiredService<IUnitOfWorkFactory>().CreateAsync())
{
    var builder = services.GetRequiredService<ISqlBuilder>();
    await SQLiteM.Orm.SchemaBootstrapper.EnsureCreatedAsync<Person>(uow, builder);
    await uow.CommitAsync();
}
```
## 4. CRUD 
```csharp
await using (var uow = await services.GetRequiredService<IUnitOfWorkFactory>().CreateAsync())
{
    var repo = services.GetRequiredService<IRepositoryFactory>().Create<Person>(uow);
    var id = await repo.InsertAsync(new Person { FirstName = "Ada", LastName = "Lovelace"});
    await uow.CommitAsync();
}
```


# High-Level: 

## SQLiteMClient

### Wenn du ohne DI/Factory auskommen möchtest:
```csharp
using SQLiteM.Orm.Pub;

// Verbindungs- oder Pfad-String
var client = new SQLiteMClient(Path.Combine(AppContext.BaseDirectory, "app.db"));

await client.EnsureCreatedAsync<Person>();

var id = await client.InsertAsync(new Person { FirstName = "Grace", LastName = "Hopper" });

await client.WithTransactionAsync(async tx =>
{
    var rp = tx.Repo<Person>();
    var p  = await rp.FindByIdAsync(id);
    p!.Email = "grace@example.com";
    await rp.UpdateAsync(p);
    await tx.CommitAsync();
});
```

## Fremdschlüssel

### Abhängige Entität:

```csharp
[Table("orders")]
public sealed class Order
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public long Id { get; set; }

    [Column("person_id", IsNullable = false)]
    [ForeignKey(typeof(Person), nameof(Person.Id), OnDelete = OnDeleteAction.Cascade)]
    public long PersonId { get; set; }

    [Column("total", IsNullable = false)]
    public decimal Total { get; set; }

    [Column("note", IsNullable = true, Length = 200)]
    public string? Note { get; set; }
}
```
### Beim Erstellen des Schemas Reihenfolge beachten:

```csharp
await SQLiteM.Orm.Pub.SQLiteMBootstrap.EnsureCreatedAsync(uow, builder, default,
    typeof(Person), typeof(Order));
```

UnitOfWork setzt PRAGMA foreign_keys = ON automatisch.

## Queries

```csharp
var items = await repo.QueryAsync(
    Query.WhereEquals("last_name", "Lovelace").OrderBy("first_name"));
```
Hinweis: Spaltennamen sind DB-Spalten, nicht CLR-Propertynamen.