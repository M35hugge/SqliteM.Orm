# Queries

SQLiteM liefert eine einfache, SQL-freie Query-API:
- `FindAllAsync()` - holt alle Zeilen der Tabelle.
- `FindByIdAsync(id)` - lädt eine Zeile per Primärschlüssel.
- `QueryAsync(Query)` - einfacher Filter/Sortierung ohne SQL.

## Grundlagen

```csharp
await using (var uow = await services.GetRequiredService<IUnitOfWorkFactory>().CreateAsync())
{
    var repo = services.GetRequiredService<IRepositoryFactory>().Create<Person>(uow);
    var all = await repo.FindAllAsync();
}
```

// Per Id

```csharp
var one = await repo.FindByIdAsync(1L);
```

## Filtern & Sortieren

Der Query-Typ erlaubt eine WHERE-Bedingung (Equals) und ORDER BY.

Wichtig: Verwende Spaltennamen (wie in [Column("...")]), nicht Propertynamen.

```csharp
// Alle "Lovelace" nach Vornamen sortiert
var people = await repo.QueryAsync(
    Query.WhereEquals("last_name", "Lovelace").OrderBy("first_name"));    
```
Mit OrderBy("... ", desc: true) kannst du absteigend sortieren:
```csharp
var latest = await repo.QueryAsync(
    new Query().OrderBy("id", desc: true));
```

## Sicherheit & Parameter

QueryAsync parametrisiert Werte automatisch:

Spaltennamen werden gequotet (Dialekt).

Werte werden als Parameter gebunden (SQL-Injection-sicher).

## Best Practices

Spaltennamen strikt aus den [Column("...")]-Attributen verwenden (z. B. "first_name" statt FirstName).

Für große Tabellen Indizes in der DB anlegen (z. B. auf last_name, person_id).

Für wiederkehrende Abfragen kleine Helper-Funktionen bauen:

```csharp
static Task<IReadOnlyList<Person>> FindByLastNameAsync(
    IRepository<Person> repo, string lastName, CancellationToken ct = default)
    => repo.QueryAsync(Query.WhereEquals("last_name", lastName).OrderBy("first_name"), ct);
```
