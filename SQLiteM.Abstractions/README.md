```md
# SQLiteM.Abstractions

Abstraktionspaket für das Micro-ORM **SQLiteM**.  
Enthält die öffentlichen **Attribute**, **Interfaces** und **Record-Typen**, die von `SQLiteM.Orm` implementiert und von Konsumenten referenziert werden.

> Dieses Paket enthält keine Implementierung. Für die konkrete ORM-Logik (Repository, UnitOfWork, SqlBuilder, Dialekt, Bootstrap, Client-Fassade) installiere zusätzlich [`SQLiteM.Orm`](https://www.nuget.org/packages/SQLiteM.Orm).

## Inhalt

- Attribute für Mapping
  - `[Table]`, `[Column]`, `[PrimaryKey]`, `[AutoIncrement]`, `[ForeignKey]`, `[Ignore]`
- Interfaces
  - `IConnectionFactory`, `ISqlDialect`, `IEntityMapper`
  - `ISqlBuilder`, `IUnitOfWork`, `IUnitOfWorkFactory`
  - `IReadRepository<T>`, `IWriteRepository<T>`, `IRepository<T>`, `IRepositoryFactory`
  - `ITransactionContext`
- Records
  - `PropertyMap` (Spalte ↔ Property)
  - `ForeignKeyMap` (FK-Beziehung)
- Hilfstyp `Query` (einfache Where/OrderBy-Abfragen)

## Installation

```bash
dotnet add package SQLiteM.Abstractions