# Unit of Work & Repository

SQLiteM stellt ein schlankes `UnitOfWork`- und `Repository<T>`-Pattern bereit:

- `UnitOfWork` kapselt Verbindung + Transaktion.
- `Repository<T>` bietet CRUD und einfache Leseabfragen für die Entität `T`.
- Über `IRepositoryFactory` erhältst du typisierte Repos aus dem DI-Container.

## Lebenszyklus eines UnitOfWork

Ein `UnitOfWork` repräsentiert einen **Transaktions-Scope**:

- `CreateAsync()` öffnet die Verbindung, setzt `PRAGMA foreign_keys = ON` und startet eine Transaktion.
- `CommitAsync()` bestätigt alle Änderungen.
- `RollbackAsync()` verwirft sie.
- Nach `CommitAsync()` oder `RollbackAsync()` den Scope beenden und für weitere Aktionen einen **neuen** UoW öffnen.

```csharp
await using (var uow = await services
    .GetRequiredService<IUnitOfWorkFactory>()
    .CreateAsync())
{
    var repo = services
        .GetRequiredService<IRepositoryFactory>()
        .Create<Person>(uow);

    var id = await repo.InsertAsync(new Person
    {
        FirstName = "Ada",
        LastName  = "Lovelace"
    });

    await uow.CommitAsync();
}
```

---

## Mehrere Repositories im selben UoW

Du kannst mehrere Repositories mit demselben UoW verwenden,  
um atomare Änderungen über mehrere Tabellen zu bündeln.

```csharp
await using (var uow = await services
    .GetRequiredService<IUnitOfWorkFactory>()
    .CreateAsync())
{
    var repoPerson = services
        .GetRequiredService<IRepositoryFactory>()
        .Create<Person>(uow);

    var repoOrder = services
        .GetRequiredService<IRepositoryFactory>()
        .Create<Order>(uow);

    var personId = await repoPerson.InsertAsync(new Person
    {
        FirstName = "Ada",
        LastName  = "Lovelace"
    });

    await repoOrder.InsertAsync(new Order
    {
        PersonId = personId,
        Total    = 19.99m,
        Note     = "Notebook"
    });

    await uow.CommitAsync();
}
```

---

## Rollback

```csharp
await using (var uow = await services
    .GetRequiredService<IUnitOfWorkFactory>()
    .CreateAsync())
{
    var repo = services
        .GetRequiredService<IRepositoryFactory>()
        .Create<Person>(uow);

    await repo.InsertAsync(new Person
    {
        FirstName = "Test",
        LastName  = "Rollback"
    });

    await uow.RollbackAsync(); // verwirft alle Änderungen
}
```

---

## Fremdschlüssel

`UnitOfWork` setzt beim Öffnen der Verbindung `PRAGMA foreign_keys = ON;`.

Bei der Schema-Erzeugung musst du die Reihenfolge beachten:  
zuerst Principal-Tabellen, dann abhängige Tabellen.

```csharp
await using (var uow = await services
    .GetRequiredService<IUnitOfWorkFactory>()
    .CreateAsync())
{
    var sqlBuilder = services.GetRequiredService<ISqlBuilder>();

    // einzelne Tabelle
    await SQLiteMBootstrap.EnsureCreatedAsync<Person>(uow, sqlBuilder);

    // weitere Tabelle mit FK auf Person
    await SQLiteMBootstrap.EnsureCreatedAsync<Order>(uow, sqlBuilder);

    await uow.CommitAsync();
}
```

---

## Häufige Stolpersteine

- Nach `CommitAsync()` denselben UoW nicht weiterverwenden – neuen Scope öffnen.
- Spaltennamen vs. Propertynamen unterscheiden (siehe „Queries“).
- Absoluten DB-Pfad verwenden (z. B. `AppContext.BaseDirectory`),  
  um Verzeichnisverwechslungen zu vermeiden.
