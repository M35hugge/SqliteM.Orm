# SQLiteM

Leichtgewichtiges, SOLID-konformes Micro-ORM für **SQLite** (C#/.NET).  
Fokus: minimale Magie, transparente SQL-Generierung, Repository + Unit of Work, Foreign Keys.

Dieses Repository enthält zwei Pakete:

- **SQLiteM.Abstractions** – öffentliche Attribute, Interfaces und Record-Typen
- **SQLiteM.Orm** – Implementierung: Repository, UnitOfWork, Dialekt, Reflection-Mapper, Bootstrap, Client-Fassade

## Features

- Attribute-Mapping: `[Table]`, `[Column]`, `[PrimaryKey]`, `[AutoIncrement]`, `[ForeignKey]`, `[Ignore]`
- Repository + Unit of Work
- DDL-Generierung (`EnsureCreated`) inkl. Fremdschlüssel
- Einfache Query-API (`Query.WhereEquals(...).OrderBy(...)`)
- Foreign Keys standardmäßig aktiv (`PRAGMA foreign_keys = ON`)
- Optionaler High-Level-Client `SQLiteMClient` (Transaktionen in einer Zeile)
- Saubere Trennung: Abstraktionen separat, Implementierungen intern

## Installation (NuGet)

Sobald veröffentlicht:

```bash
dotnet add package SQLiteM.Abstractions
dotnet add package SQLiteM.Orm
```

## Getting Started

### 1) Services registrieren (DI)

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

### 2) Entitäten definieren

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

### 3) Schema erzeugen

```csharp
await using (var uow = await services.GetRequiredService<IUnitOfWorkFactory>().CreateAsync())
{
    var builder = services.GetRequiredService<ISqlBuilder>();
    await SQLiteM.Orm.Pub.SQLiteMBootstrap.EnsureCreatedAsync<Person>(uow, builder);
    await uow.CommitAsync();
}
```

### 4) CRUD

```csharp
await using (var uow = await services.GetRequiredService<IUnitOfWorkFactory>().CreateAsync())
{
    var repo = services.GetRequiredService<IRepositoryFactory>().Create<Person>(uow);

    var id = await repo.InsertAsync(new Person
    {
        FirstName = "Ada",
        LastName  = "Lovelace",
        Email     = "ada@example.com"
    });

    var p = await repo.FindByIdAsync(id);
    p!.Email = "ada.lovelace@history.org";
    await repo.UpdateAsync(p);

    await repo.DeleteAsync(id);
    await uow.CommitAsync();
}
```

## High-Level Client (`SQLiteMClient`)

Falls du ohne DI auskommen möchtest:

```csharp
using SQLiteM.Orm.Pub;

var client = new SQLiteMClient(Path.Combine(AppContext.BaseDirectory, "app.db"));

await client.EnsureCreatedAsync<Person>();

var id = await client.InsertAsync(new Person { FirstName = "Grace", LastName = "Hopper" });

await client.WithTransactionAsync(async tx =>
{
    var rp = tx.Repo<Person>();
    var p  = await rp.FindByIdAsync(id);
    p!.Email = "grace@example.com";
    await rp.UpdateAsync(p);
});
```

## Fremdschlüssel & Reihenfolge

```csharp
[Table("orders")]
public sealed class Order
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [Column("person_id", IsNullable = false)]
    [ForeignKey(typeof(Person), nameof(Person.Id), OnDelete = OnDeleteAction.Cascade)]
    public long PersonId { get; set; }

    [Column("total", IsNullable = false)]
    public decimal Total { get; set; }

    [Column("note", IsNullable = true, Length = 200)]
    public string? Note { get; set; }
}
```

Schema-Erzeugung in Reihenfolge (Principal → Dependent):

```csharp
await SQLiteM.Orm.Pub.SQLiteMBootstrap.EnsureCreatedAsync(
    uow, builder, default,
    typeof(Person), typeof(Order));
```

