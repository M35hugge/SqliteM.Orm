# SQLiteM – Release Checklist (v0.1.0)

## 1) MUST vor 0.1.0

### API-Stabilität
- API-Stabilität
	- SQLiteM.Abstractions: nur das Nötigste public (Interfaces, Attribute, Records).
	- Implementierungen intern halten (.Internal), Factories/Fassade public.
	- Namenskonventionen klar (Spalte vs. Property, Quote/ParameterPrefix).

### Transaktionen & Unit of Work
- IUnitOfWorkFactory öffnet Transaktion, UnitOfWork setzt PRAGMA foreign_keys = ON.
- Kein Weiterverwenden eines IUnitOfWork nach CommitAsync().

### Insert/Update/Delete korrekt

- InsertAsync gibt die neue Id zurück und setzt sie am Objekt.

- SqlBuilder schließt PK/AutoIncrement beim INSERT/UPDATE aus.

### Mapping robust

- PropertyMap.PropertyName = CLR-Property, ColumnName = DB-Spalte.

- FK-Mapping erzeugt gültiges DDL (kein überzähliges Semikolon).

### Parameterisierung
- Ausnahmslos parametrisierte SQL-Befehle. Kein String-Concat in WHERE.

### Disposal

- UnitOfWork.DisposeAsync rollt ungecommittete Tx zurück, entsorgt Transaktion/Connection zuverlässig (auch bei Exceptions).

### Nullable & Warnings
- <Nullable>enable</Nullable> und möglichst TreatWarningsAsErrors.
- 
### Dokumentation
- README (Quick Start, Beispiele, Limits), DocFX baut lokal sauber.

### NuGet-Metadaten
- Lizenz (z. B. MIT), Repository-URL, Project-URL, Tags, Icon, README, SourceLink, Symbols.

---

## 2) SHOULD kurz nach 0.1.0

### Logging & Diagnostik
- Optionales `ILogger` für SQL/Timing, oder `ActivitySource` für OpenTelemetry.

### Optionen
- `SQLiteMOptions` erweitert: PRAGMA-Tuning (`journal_mode=WAL`, `synchronous=NORMAL`), Timeouts.

### Fehlermeldungen
- Konsistente, aussagekräftige Exceptions (z. B. Missing PK Mapping, Property not found).

### Query-Erweiterungen
- Mehrfach-Where, Vergleichsoperatoren (`<`, `>`, `LIKE`), `OrderByMultiple`, Paging (`LIMIT/OFFSET`) im `Query`.

### Batch-Operationen
- `InsertRangeAsync`, `UpdateRangeAsync`, `DeleteRangeAsync`.

### Multi-Targeting
- `net8.0;net9.0`.

### Typkonverter
- `decimal` Rundung prüfen, `Guid`, `enum`, `DateTimeOffset` Roundtrip.

### Mapping-Validierung
- `ValidateMappings<T>()` optional, prüft:
  - PK vorhanden, Property existiert.
  - FK-Ziel existiert und verweist auf PK/UNIQUE.
  - Alle gemappten Properties sind öffentlich les-/schreibbar.

---

## 3) NICE später

- Einfache Migrations-Helfer (AddColumnIfMissing, CreateIndexIfMissing).
- Mini-LINQ-Expressions für WHERE/ORDER BY.
- Optimistic Concurrency (RowVersion/UpdatedAt).
- Ambient Transaction-Context (AsyncLocal) mit Weiterverwendung des UoW in verschachtelten Aufrufen.

---

## 4) Test-Matrix

### CRUD & PK
- Insert/Read/Update/Delete mit `[PrimaryKey, AutoIncrement]`.
- Insert mit vorab gesetzter Id (nicht AI) – korrekt behandelt.
- Update ohne Id führt zu klarer Exception.
- Delete nicht vorhandener Id → 0 Rows.

### Foreign Keys
- Insert `Order` mit gültiger und ungültiger `PersonId` (Constraint failed).
- Cascade Delete: Person löschen → Orders leer.
- RESTRICT/SET NULL/SET DEFAULT (sofern im Dialekt/Mappings angelegt) verifizieren.

### DDL
- `EnsureCreated<Person>()` erzeugt erwartete SQL (aus `sqlite_master` verifizieren).
- Reihenfolge: erst Principal, dann Dependent (`Person` vor `Order`).

### Transaktionen
- Commit vs. Rollback: Zweiter Insert schlägt fehl → nichts persistiert.
- `WithTransactionAsync` und `BeginTransactionAsync`:
  - Erfolgsweg: Commit.
  - Fehlerweg: Rollback.

### Query
- `WhereEquals` + `OrderBy`, leere Resultsets.
- Falscher Spaltenname in `Query` → klare Fehlermeldung.
- Optional: Paging-Verhalten (wenn implementiert).

### Robustheit
- Pro Test eine frische DB-Datei (`Path.GetTempPath()` + `Guid`).
- Parallel laufende Tests.
- Decimal Roundtrip (10.10m, 0.1m etc.).
- Pfadseparatoren und CI unter Windows/Linux.

---

## 5) Packaging (csproj-Beispiel)

```xml
<PropertyGroup>
  <TargetFrameworks>net8.0;net9.0</TargetFrameworks>
  <Nullable>enable</Nullable>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>

  <PackageId>SQLiteM</PackageId>
  <Version>0.1.0</Version>
  <Authors>Dein Name</Authors>
  <Description>Leichtgewichtiges Micro-ORM für SQLite (Attributes, Repository/UoW, EnsureCreated, einfache Queries).</Description>
  <PackageTags>sqlite;orm;micro-orm;repository;unit-of-work</PackageTags>
  <PackageProjectUrl>https://github.com/dein/repo</PackageProjectUrl>
  <RepositoryUrl>https://github.com/dein/repo</RepositoryUrl>
  <RepositoryType>git</RepositoryType>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <PackageReadmeFile>README.md</PackageReadmeFile>
  <PackageIcon>icon.png</PackageIcon>

  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
  <Deterministic>true</Deterministic>
  <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
</PropertyGroup>

<ItemGroup>
  <None Include="README.md" Pack="true" PackagePath="\" />
  <None Include="icon.png" Pack="true" PackagePath="\" />
</ItemGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.Data.Sqlite" Version="8.*" />
  <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.*" PrivateAssets="All" />
</ItemGroup>
```