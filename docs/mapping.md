# Mapping (Attribute, Fremdschlüssel, Typen)

## Tabelle & Spalten

```csharp
[Table("persons")]
public sealed class Person
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public long Id { get; set; }

    [Column("first_name", IsNullable = false, Length = 100)]
    public string FirstName { get; set; } = default!;

    [Column("email", IsNullable = true, Length = 255)]
    public string? Email { get; set; }
}
```

Table(name): Tabellenname in der DB

Column(name, IsNullable, Length): Spaltenname, NULL-Zulässigkeit, optionale Länge (bei string)

PrimaryKey/AutoIncrement: Primärschlüssel, Autowert

SQLite-Hinweis: Längenangaben (VARCHAR(n)) sind deklarativ; SQLite erzwingt die Länge nicht, sie verbessern aber Lesbarkeit/Portabilität.

## Fremdschlüssel

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


ForeignKey(principalEntity, principalColumn): definiert die Beziehung.

OnDelete: NoAction, Restrict, Cascade, SetNull, SetDefault.

Die referenzierte Spalte muss PRIMARY KEY oder UNIQUE sein (bei Person.Id gegeben).


### DDL-Erzeugung: BuildCreateTable(typeof(Order)) erzeugt

```csharp
FOREIGN KEY ("person_id") REFERENCES "persons" ("id") ON DELETE CASCADE
```
Aktivierung: Das ORM schaltet PRAGMA foreign_keys = ON; bei UnitOfWork automatisch ein.

## Navigationen
```csharp

[Ignore] public List<Order> Orders { get; } = new();
```

[Ignore] schliesst reine Navigations-Properties von DDL/CRUD aus.

Typabbildung (Default)
C#-Typ	SQLite-Typ
int, long, bool	INTEGER
double, float, decimal	REAL
string, DateTime, DateTimeOffset	TEXT

## Reihenfolge bei DDL

FK-abhängige Tabellen nach ihren Principals erzeugen:
```csharp
await EnsureCreatedAsync<Person>(...);
await EnsureCreatedAsync<Order>(...);
```