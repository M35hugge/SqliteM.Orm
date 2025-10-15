# 🧩 SQLiteM – leichtgewichtiges ORM für SQLite

SQLiteM ist ein einfaches, attributbasiertes ORM für .NET-Anwendungen,  
das ein **Unit-of-Work**-Pattern mit einem **Repository-System** kombiniert – optimiert für **SQLite**.

---

## 🚀 Schnellstart

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

---

| Kategorie                                    | Beschreibung                                |
| -------------------------------------------- | ------------------------------------------- |
| [Getting Started](getting-started.md)        | Einführung, Installation und erste Schritte |
| [Mapping](mapping.md)                        | Attribute & Entitätskonfiguration           |
| [Unit of Work & Repository](unit-of-work.md) | Transaktionssteuerung und Datenzugriff      |
| [Queries](queries.md)                        | Einfache Filter- und Sortierabfragen        |
| [API](api/index.md)                          | Vollständige API-Referenz                   |

---
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


💬 Warum SQLiteM?

Minimalistisch: Nur, was du wirklich brauchst.

Einfach konfigurierbar: Keine komplizierten Migrations-Tools.

Schnell & leicht: Ideal für Embedded- oder Desktop-Apps.

Vollständig async: Kompatibel mit modernen .NET-Patterns.



---

## ✨ Hinweise zur Integration

1. Ersetze die Datei `index.md` im Wurzelverzeichnis deines DocFX-Dokuments (meist `/docs/index.md` oder `/docfx_project/articles/index.md`) durch den obigen Inhalt.  
2. Stelle sicher, dass in `docfx.json` im Abschnitt `"content"` der Pfad zur `index.md` enthalten ist.  
3. Optional:  
   - Füge im `docfx.json` unter `"globalMetadata"` z. B. `"title": "SQLiteM Dokumentation"` hinzu.  
   - Wenn du ein Logo oder ein Favicon möchtest, kannst du das unter `"template"` → `"default"` → `logo` konfigurieren.

---

Möchtest du, dass ich dir im nächsten Schritt ein **passendes Farb- und Layoutkonzept** für den DocFX-Template-Ordner (`templates/`) zusammenstelle – z. B. angepasste Kopfzeile, Akzentfarbe und Schrift?  
(Damit kann die Seite aussehen wie ein modernes Framework-Docs-Portal, z. B. Dapper oder EF Core-ähnlich.)
