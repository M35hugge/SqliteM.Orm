# Transaktionen mit SQLiteMClient.WithTransactionAsync

`SQLiteMClient` ist die High-Level-Fassade von SQLiteM:
- kümmert sich intern um DI/ServiceProvider,
- öffnet Verbindungen und Transaktionen,
- stellt dir über `WithTransactionAsync` einen einfachen Einstiegspunkt für atomare Operationen zur Verfügung. :contentReference[oaicite:0]{index=0}

Diese Seite zeigt, wie du `WithTransactionAsync` in typischen Szenarien nutzt.

---

## 1. Wann `WithTransactionAsync`?

Verwende `SQLiteMClient.WithTransactionAsync`, wenn du:

- **kein DI/ServiceProvider** in deiner Anwendung verwenden willst (z. B. Konsolen-Tools, kleine Utilities),
- mehrere Tabellen in **einer** Transaktion bearbeiten möchtest,
- nicht selbst `IUnitOfWorkFactory` und `IRepositoryFactory` verkabeln willst.

Wenn du bereits DI im Einsatz hast und deine Services sowieso `IUnitOfWorkFactory`/`IRepositoryFactory` injizieren, kannst du auch direkt das Unit-of-Work-Pattern verwenden (siehe „Unit of Work & Repository“). :contentReference[oaicite:1]{index=1}

---

## 2. Setup & Client

Minimaler Einstieg:

```csharp
using SQLiteM.Orm.Pub;

var cs = "Data Source=app.db;Cache=Shared";

// High-Level Client (Zero-DI)
await using var client = new SQLiteMClient(cs);

// Schema erzeugen (einmalig)
await client.EnsureCreatedAsync<Person>();
```

## 3. Einfache Transaktion mit einem Repository

```csharp
// Insert außerhalb der Transaktion (zur Vereinfachung des Beispiels)
var id = await client.InsertAsync(new Person
{
    FirstName = "Grace",
    LastName  = "Hopper"
});

// Alles, was hier passiert, läuft in einer Transaktion
await client.WithTransactionAsync(async tx =>
{
    // typisiertes Repository für Person holen
    var people = tx.Repo<Person>();

    var p = await people.FindByIdAsync(id);
    if (p is null)
        return; // nichts zu tun

    p.Email = "grace@example.com";
    await people.UpdateAsync(p);

    // alle Änderungen bestätigen
    await tx.CommitAsync();
});
```

Hier kommt tx aus dem Interface ITransactionContext:

 - Repo<T>() liefert ein IRepository<T> für die aktuelle Transaktion,
 - CommitAsync() bestätigt alle Änderungen,
 - RollbackAsync() bricht ab und verwirft alles,
 - IsCompleted zeigt, ob Commit oder Rollback bereits erfolgt sind,
 - Uow gibt dir (falls nötig) Zugriff auf die zugrunde liegende IUnitOfWork.

## 4. Mehrere Repositories in einer Transaktion

Du kannst innerhalb derselben Transaktion beliebig viele Repositories verwenden:

```csharp
await client.WithTransactionAsync(async tx =>
{
    var people = tx.Repo<Person>();
    var orders = tx.Repo<Order>();

    // neue Person anlegen
    var personId = await people.InsertAsync(new Person
    {
        FirstName = "Ada",
        LastName  = "Lovelace"
    });

    // passende Order einfügen
    await orders.InsertAsync(new Order
    {
        PersonId = personId,
        Total    = 19.99m,
        Note     = "Notebook"
    });

    // alles zusammen committen
    await tx.CommitAsync();
});
```
Alle Inserts/Updates/Deletes, die über tx.Repo<T>() laufen, werden in einem Commit bestätigt.

## 5. Rollback mit WithTransactionAsync

Wenn innerhalb des Delegates etwas schiefgeht, kannst du explizit einen Rollback auslösen:
```csharp
await client.WithTransactionAsync(async tx =>
{
    var people = tx.Repo<Person>();

    await people.InsertAsync(new Person
    {
        FirstName = "Test",
        LastName  = "Rollback"
    });

    // Business-Regel verletzt → Änderungen verwerfen
    await tx.RollbackAsync();
});
```
ITransactionContext ist für genau einen Lebenszyklus gedacht:

 - Commit oder Rollback,
 - danach gilt der Kontext als abgeschlossen (IsCompleted = true) und darf nicht weiterverwendet werden. 

## 6. Fehlerszenarien & Best Practices
Commit nicht vergessen

Rufe am Ende deiner Transaktionslogik immer tx.CommitAsync() auf, wenn alles erfolgreich war.

Nutze tx.RollbackAsync(), wenn du bewusst alles zurückdrehen willst (z. B. wegen Validierungsfehlern).

Pro parallelem Vorgang einen Kontext

ITransactionContext (und die dazugehörigen Repositories) sind nicht threadsicher.
Wenn du parallel arbeitest, verwende pro Task/Scope einen eigenen Aufruf von WithTransactionAsync.

Spaltennamen in Queries

Auch innerhalb von WithTransactionAsync gelten die normalen Query-Regeln:

```csharp
var latestOrders = await tx.Repo<Order>().QueryAsync(
    Query
        .WhereEquals("person_id", 42)
        .OrderBy("id", desc: true));
```

 - Verwende DB-Spaltennamen wie person_id, last_name etc. 
 - Der konfigurierte INameTranslator versucht, CLR-Namen wie PersonId trotzdem sinnvoll aufzulösen, falls du dich vertippst.

## 7. Wann lieber direkt mit IUnitOfWork arbeiten?

SQLiteMClient.WithTransactionAsync ist ideal für:
  - kleine Scripte,
  - Konsolen-Tools,
  - Test-Utilities,
  - Apps ohne DI.

Wenn du eine größere Anwendung mit Dependency Injection hast, ist in der Regel das explizite Arbeiten mit:
 - IUnitOfWorkFactory,
 - IRepositoryFactory

die flexiblere Variante – insbesondere, wenn du Transaktionen über mehrere Services/Layer hinweg kontrollieren willst.