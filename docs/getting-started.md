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
