# Unit of Work & Repository

SQLiteM stellt ein schlankes `UnitOfWork`- und `Repository<T>`-Pattern bereit:
- `UnitOfWork` kapselt Verbindung + Transaktion.
- `Repository<T>` bietet CRUD und einfache Leseabfragen für die Entität `T`.
- über `IRepositoryFactory` erhältst du typisierte Repos aus dem DI-Container.

## Lebenszyklus eines UnitOfWork

Ein `UnitOfWork` repräsentiert einen **Transaktions-Scope**:
- Beginnt mit `CreateAsync()` automatisch eine Transaktion.
- `CommitAsync()` bestätigt alle änderungen.
- `RollbackAsync()` verwirft sie.
- Nach `CommitAsync()` den Scope beenden und für weitere Aktionen einen **neuen** UoW oeffnen.

```csharp
await using (var uow = await services.GetRequiredService<IUnitOfWorkFactory>().CreateAsync())
{
    var repo = services.GetRequiredService<IRepositoryFactory>().Create<Person>(uow);

    var id = await repo.InsertAsync(new Person { FirstName = "Ada", LastName = "Lovelace" });
    await uow.CommitAsync();
}
```

## Mehrere Repositories im selben UoW

Du kannst mehrere Repositories mit demselben UoW verwenden, um atomare änderungen über mehrere Tabellen zu bündeln.
```csharp


await using (var uow = await services.GetRequiredService<IUnitOfWorkFactory>().CreateAsync())
{
    var repoPerson = services.GetRequiredService<IRepositoryFactory>().Create<Person>(uow);
    var repoOrder  = services.GetRequiredService<IRepositoryFactory>().Create<Order>(uow);

    var personId = await repoPerson.InsertAsync(new Person { FirstName = "Ada", LastName = "Lovelace" });
    await repoOrder.InsertAsync(new Order { PersonId = personId, Total = 19.99m, Note = "Notebook" });

    await uow.CommitAsync();
}
```
## Rollback

```csharp
await using (var uow = await services.GetRequiredService<IUnitOfWorkFactory>().CreateAsync())
{
    var repo = services.GetRequiredService<IRepositoryFactory>().Create<Person>(uow);

    await repo.InsertAsync(new Person { FirstName = "Test", LastName = "Rollback" });
    await uow.RollbackAsync(); // verwirft alle Änderungen
}
```

## Fremdschlüssel

UnitOfWork setzt beim Öffnen der Verbindung PRAGMA foreign_keys = ON;.

Erzeugungsreihenfolge beachten: zuerst Principal-Tabellen, dann abhängige Tabellen.

```csharp
await using (var uow = await services.GetRequiredService<IUnitOfWorkFactory>().CreateAsync())
{
    var b = services.GetRequiredService<ISqlBuilder>();
    await SchemaBootstrapper.EnsureCreatedAsync<Person>(uow, b);
    await SchemaBootstrapper.EnsureCreatedAsync<Order>(uow, b);
    await uow.CommitAsync();
}
```

## Häufige Stolpersteine

Nach CommitAsync() denselben UoW nicht weiterverwenden; neuen Scope öffnen.

Spaltennamen vs. Propertynamen unterscheiden (siehe 'Queries').

Absoluten DB-Pfad verwenden (AppContext.BaseDirectory), um Verzeichnisverwechslungen zu vermeiden.