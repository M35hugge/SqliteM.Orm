# Getting Started

## 1. Services und Client registrieren
```csharp
using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using SQLiteM.Orm;

var dbPath = Path.Combine(AppContext.BaseDirectory, "app.db");
var cs = $"Data Source={dbPath};Cache=Shared";
var services = new ServiceCollection()
    .AddSQLiteM(o => o.ConnectionString = cs)
    .BuildServiceProvider();
var client = new SQLiteMClient(cs);
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
client.EnsureCreatedAsync<Person>();
```
## 4. CRUD 
```csharp
    //Insert
    var id = await client.InsertAsync(new Person { FirstName = "Ada", LastName = "Lovelace"});
    
    //GetById
    var person = await client.FindByIdAsync(id);

    //Update
    person.FirsName = "Bda";
    var newPerson=await client.UpdateAsync(person);

    //Delelte
    await client.DeleteAsync(id);
    
```
