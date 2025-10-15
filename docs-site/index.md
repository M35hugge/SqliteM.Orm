# SQLiteM – leichtgewichtiges ORM für SQLite

SQLiteM ist ein einfaches, attributbasiertes ORM für .NET-Anwendungen,  
das ein **Unit-of-Work**-Pattern mit einem **Repository-System** kombiniert – optimiert für **SQLite**.

---

## Schnellstart

```csharp
// Program.cs
using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Orm;

var services = new ServiceCollection()
    .AddSQLiteM(opt => opt.ConnectionString = "Data Source=app.db")
    .BuildServiceProvider();

// Unit-of-Work erstellen
using var scope = services.CreateScope();
var uowFactory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
using var uow = await uowFactory.CreateAsync();

await SQLiteMBootstrap.EnsureCreatedAsync<User>(uow, 
    scope.ServiceProvider.GetRequiredService<ISqlBuilder>());
```

---

## Themenübersicht

| Kategorie | Beschreibung |
|------------|---------------|
| [Getting Started](articles/getting-started) | Einführung, Installation und erste Schritte |
| [Mapping](articles/mapping) | Attribute & Entitätskonfiguration |
| [Unit of Work & Repository](articles/unit-of-work-repository) | Transaktionssteuerung und Datenzugriff |
| [Queries](articles/queries) | Einfache Filter- und Sortierabfragen |
| [API](api/index) | Vollständige API-Referenz |

---

## Architekturüberblick

```
+-----------------+
| Application     |
| (nutzt ORM API) |
+-----------------+
        │
        ▼
+-----------------+
| SQLiteM.Orm     |
|  - Repository   |
|  - UnitOfWork   |
|  - SqlBuilder   |
+-----------------+
        │
        ▼
+-----------------+
| SQLiteM.Abstractions |
|  - Interfaces         |
|  - Attribute          |
+-----------------+
```

---

## Warum SQLiteM?

- **Minimalistisch:** Nur, was du wirklich brauchst.  
- **Einfach konfigurierbar:** Keine komplizierten Migrations-Tools.  
- **Schnell & leicht:** Ideal für Embedded- oder Desktop-Apps.  
- **Vollständig async:** Kompatibel mit modernen .NET-Patterns.  

---

© SQLiteM Projekt · Dokumentation mit [DocFX](https://dotnet.github.io/docfx)
