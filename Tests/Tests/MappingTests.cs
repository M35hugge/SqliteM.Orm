using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using SQLiteM.Orm.Pub;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tests.Helpers;
using Xunit;

namespace Tests.Tests
{
    public class MappingTests
    {
        // ---------- 1) Implizites Mapping ohne [Column] mit Identity-Translator ----------
        // Erwartung: Spaltennamen == CLR-Propertynamen
        [Fact]
        public async Task ImplicitMapping_Identity_Roundtrip_Works()
        {
            using var sp = TestHost.CreateProvider(out _); // IdentityNameTranslator als Default
            await TestHost.WithUowAsync(sp, async uow =>
            {
                var b = sp.GetRequiredService<ISqlBuilder>();
                await SQLiteMBootstrap.EnsureCreatedAsync<IdentityPerson>(uow, b);
                await uow.CommitAsync();
            });

            // Insert & Read
            await TestHost.WithUowAsync(sp, async uow =>
            {
                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<IdentityPerson>(uow);
                var id = await repo.InsertAsync(new IdentityPerson { FirstName = "Ada", LastName = "Lovelace" });
                Assert.True(id > 0);
                await uow.CommitAsync();
            });

            await TestHost.WithUowAsync(sp, async uow =>
            {
                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<IdentityPerson>(uow);
                var all = await repo.FindAllAsync();
                Assert.Single(all);
                Assert.Equal("Ada", all[0].FirstName);
                Assert.Equal("Lovelace", all[0].LastName);
            });
        }

        // ---------- 2) Implizites Mapping mit SnakeCaseNameTranslator (Property -> snake_case) ----------
        // Erwartung: Tabelle und Spalten werden nach snake_case übersetzt; Rückübersetzung (Reader -> CLR) funktioniert.
        [Fact]
        public async Task ImplicitMapping_SnakeCase_Roundtrip_Works()
        {
            using var sp = TestHost.CreateProvider(new SnakeCaseNameTranslator());

            // Schema anlegen & committen
            await TestHost.WithUowAsync(sp, async uow =>
            {
                var b = sp.GetRequiredService<ISqlBuilder>();
                await SQLiteMBootstrap.EnsureCreatedAsync<SnakePerson>(uow, b);
                await uow.CommitAsync();
            });

            // Insert
            await TestHost.WithUowAsync(sp, async uow =>
            {
                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<SnakePerson>(uow);
                var id = await repo.InsertAsync(new SnakePerson { FirstName = "Grace", LastName = "Hopper" });
                Assert.True(id > 0);
                await uow.CommitAsync();
            });

            // Verify: Rückübersetzung (snake_case -> CamelCase) + Tabellenname existiert
            await TestHost.WithUowAsync(sp, async uow =>
            {
                // Tabelle muss per Translator "snake_person" heißen (da kein [Table]-Name angegeben)
                using (var cmd = uow.Connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='snake_person'";
                    var name = cmd.ExecuteScalar() as string;
                    Assert.Equal("snake_person", name);
                }

                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<SnakePerson>(uow);
                var all = await repo.FindAllAsync();
                Assert.Single(all);
                Assert.Equal("Grace", all[0].FirstName);
                Assert.Equal("Hopper", all[0].LastName);
            });
        }

        // ---------- 3) Kollisionserkennung: 2 CLR-Properties -> gleicher Spaltenname nach Translation ----------
        // Erwartung: InvalidOperationException beim Ermitteln der Spaltenmaps / Schemaerzeugung
        [Fact]
        public async Task ColumnNameCollision_AfterTranslation_Throws()
        {
            // Translator EXPLIZIT setzen, sonst passiert ggf. keine Kollision
            using var sp = TestHost.CreateProvider(new SnakeCaseNameTranslator());

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await TestHost.WithUowAsync(sp, async uow =>
                {
                    var b = sp.GetRequiredService<ISqlBuilder>();
                    // ruft intern _mapper.GetPropertyMaps(...) auf → sollte die Kollision werfen
                    await SQLiteMBootstrap.EnsureCreatedAsync<PersonCollision>(uow, b);
                });
            });
        }

        // ---------- 4) Table-Name Fallback (mit und ohne [Table]) ----------
        // Erwartung: Ohne [Table(Name=...)] wird Translator für den Typnamen genutzt.
        [Fact]
        public async Task TableName_Fallback_Works_With_And_Without_TableAttribute()
        {
            using var sp = TestHost.CreateProvider(new SnakeCaseNameTranslator());

            await TestHost.WithUowAsync(sp, async uow =>
            {
                var b = sp.GetRequiredService<ISqlBuilder>();
                await SQLiteMBootstrap.EnsureCreatedAsync<ExplicitTableEntity>(uow, b);  // [Table("people_explicit")]
                await SQLiteMBootstrap.EnsureCreatedAsync<NoTableEntity>(uow, b);        // => "no_table_entity" (snake_case)
                await uow.CommitAsync();
            });

            await TestHost.WithUowAsync(sp, async uow =>
            {
                var names = new List<string>();
                using var cmd = uow.Connection.CreateCommand();
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
                using var r = cmd.ExecuteReader();
                while (r.Read()) names.Add(r.GetString(0));

                Assert.Contains("people_explicit", names);   // explizit
                Assert.Contains("no_table_entity", names);   // Fallback über Translator
            });
        }

        // ---------- Entities für die Tests ----------

        // 1) Identity-Translator: Spalten = Propertynamen
        [Table("IdentityPerson")]
        public sealed class IdentityPerson
        {
            [PrimaryKey, AutoIncrement] public int Id { get; set; }
            public string FirstName { get; set; } = default!;
            public string LastName { get; set; } = default!;
            public string? Email { get; set; }
        }

        // 2) SnakeCase-Translator, kein [Table]-Name, Propertynamen werden übersetzt
        public sealed class SnakePerson
        {
            [PrimaryKey, AutoIncrement] public int Id { get; set; }
            public string FirstName { get; set; } = default!;
            public string LastName { get; set; } = default!;
        }

        private sealed class PersonCollision
        {
            // → "first_name" (via SnakeCase)
            public string FirstName { get; set; } = "";

            // → "first_name" (via SnakeCase; Unterstrich wird „verschluckt“)
            public string First_Name { get; set; } = "";
        }

        // 4) TableName-Fallback
        [Table("people_explicit")]
        public sealed class ExplicitTableEntity
        {
            [PrimaryKey, AutoIncrement] public int Id { get; set; }
        }

        // kein [Table] -> Translator(Table(type.Name)) => "no_table_entity"
        public sealed class NoTableEntity
        {
            [PrimaryKey, AutoIncrement] public int Id { get; set; }
        }
    }
}
