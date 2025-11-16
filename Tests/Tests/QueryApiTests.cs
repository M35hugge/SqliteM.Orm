using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using SQLiteM.Orm.Pub;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tests.Entities;
using Tests.Helpers;
using Xunit;

namespace Tests.Tests;

public class QueryTests
{
    private static async Task SeedAsync(ServiceProvider sp)
    {
        await TestHost.WithUowAsync(sp, async uow =>
        {
            var b = sp.GetRequiredService<ISqlBuilder>();
            await SQLiteMBootstrap.EnsureCreatedAsync<PersonQ>(uow, b);

            var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonQ>(uow);

            var t0 = new DateTime(2025, 1, 1);
            await repo.InsertAsync(new PersonQ { FirstName = "Ada", LastName = "Lovelace", Age = 36, CreatedAt = t0.AddDays(0), Email = "ada@example.com" });
            await repo.InsertAsync(new PersonQ { FirstName = "Grace", LastName = "Hopper", Age = 85, CreatedAt = t0.AddDays(1), Email = null });
            await repo.InsertAsync(new PersonQ { FirstName = "Alan", LastName = "Turing", Age = 41, CreatedAt = t0.AddDays(2), Email = "alan@example.com" });
            await repo.InsertAsync(new PersonQ { FirstName = "Barbara", LastName = "Liskov", Age = 30, CreatedAt = t0.AddDays(3), Email = null });

            await uow.CommitAsync();
        });
    }

    [Fact]
    public async Task WhereGreater_Works()
    {
        using var sp = TestHost.CreateProvider(out _);
        await SeedAsync(sp);

        await TestHost.WithUowAsync(sp, async uow =>
        {
            var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonQ>(uow);

            var q = Query.WhereGreater("age", 40);
            var rows = await repo.QueryAsync(q);

            Assert.All(rows, r => Assert.True(r.Age > 40));
            Assert.Contains(rows, r => r.FirstName == "Grace");
            Assert.Contains(rows, r => r.FirstName == "Alan");
            Assert.DoesNotContain(rows, r => r.FirstName == "Ada");
        });
    }

    [Fact]
    public async Task WhereGreaterOrEquals_And_OrderBy_Desc_Works()
    {
        using var sp = TestHost.CreateProvider(out _);
        await SeedAsync(sp);

        await TestHost.WithUowAsync(sp, async uow =>
        {
            var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonQ>(uow);

            var q = Query.WhereGreaterOrEquals("age", 36)
                         .OrderBy("created_at", desc: true); // DB-Spaltenname
            var rows = await repo.QueryAsync(q);

            Assert.All(rows, r => Assert.True(r.Age >= 36));
            // Prüfe absteigende Sortierung nach CreatedAt:
            Assert.True(rows.Zip(rows.Skip(1), (a, b) => a.CreatedAt >= b.CreatedAt).All(x => x));
        });
    }

    [Fact]
    public async Task WhereLess_AndEquals_Works_With_AND()
    {
        using var sp = TestHost.CreateProvider(out _);
        await SeedAsync(sp);

        await TestHost.WithUowAsync(sp, async uow =>
        {
            var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonQ>(uow);

            // AND-Kette: Age < 40 UND LastName = "Lovelace"
            var q = Query.WhereLess("age", 40)
                         .AndEquals("last_name", "Lovelace");
            var rows = await repo.QueryAsync(q);

            Assert.Single(rows);
            Assert.Equal("Ada", rows[0].FirstName);
        });
    }

    [Fact]
    public async Task WhereLessOrEquals_With_ClrPropertyName_Resolves_To_DB()
    {
        using var sp = TestHost.CreateProvider(out _);
        await SeedAsync(sp);

        await TestHost.WithUowAsync(sp, async uow =>
        {
            var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonQ>(uow);

            // CLR-Property-Namen (werden vom Repo per ResolveColumnName auf DB-Spalten gemappt)
            var q = Query.WhereLessOrEquals("Age", 36)                       // CLR
                         .AndGreater("CreatedAt", new DateTime(2025, 1, 1)); // CLR
            var rows = await repo.QueryAsync(q);

            Assert.All(rows, r => Assert.True(r.Age <= 36 && r.CreatedAt > new DateTime(2025, 1, 1)));
        });
    }

    [Fact]
    public async Task Equals_Null_Produces_IS_NULL()
    {
        using var sp = TestHost.CreateProvider();

        // 1) Schema anlegen & committen
        await TestHost.WithUowAsync(sp, async uow =>
        {
            var b = sp.GetRequiredService<ISqlBuilder>();
            await SQLiteM.Orm.Pub.SQLiteMBootstrap.EnsureCreatedAsync<PersonQ>(uow, b);
            await uow.CommitAsync();
        });

        // 2) Testdaten – eine Zeile mit Email = null
        await TestHost.WithUowAsync(sp, async uow =>
        {
            var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonQ>(uow);
            await repo.InsertAsync(new PersonQ { FirstName = "Ada", LastName = "L.", Email = null });
            await repo.InsertAsync(new PersonQ { FirstName = "Grace", LastName = "H.", Email = "grace@example.com" });
            await uow.CommitAsync();
        });

        // 3) Query: IS NULL auf "email"
        await TestHost.WithUowAsync(sp, async uow =>
        {
            var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonQ>(uow);
            var q = Query.WhereEquals("email", null); // DB-Spaltenname oder CLR geht, wird gemappt
            var rows = await repo.QueryAsync(q);

            Assert.Single(rows);
            Assert.Equal("Ada", rows[0].FirstName);
        });
    }


    [Fact]
    public async Task Query_Uses_Parameters_NoInjection()
    {
        using var sp = TestHost.CreateProvider(out _);
        await SeedAsync(sp);
        await TestHost.WithUowAsync(sp, async uow =>
        {
            var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonQ>(uow);
            var evil = "Alan'; DROP TABLE people_q; --";
            var q = Query.WhereEquals("first_name", evil);
            var rows = await repo.QueryAsync(q);
            Assert.Empty(rows);

            // Tabelle existiert noch:
            var all = await repo.FindAllAsync();
            Assert.NotEmpty(all);
        });
    }

    [Fact]
    public async Task OrderBy_ClrPropertyName_Works()
    {
        using var sp = TestHost.CreateProvider(out _);
        await SeedAsync(sp);
        await TestHost.WithUowAsync(sp, async uow =>
        {
            var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonQ>(uow);
            var q = Query.WhereGreaterOrEquals("Age", 0).OrderBy("CreatedAt", desc: true);
            var rows = await repo.QueryAsync(q);
            Assert.True(rows.Zip(rows.Skip(1), (a, b) => a.CreatedAt >= b.CreatedAt).All(x => x));
        });
    }

    [Fact]
    public async Task Unknown_Column_Throws()
    {
        using var sp = TestHost.CreateProvider(out _);
        await SeedAsync(sp);

        await TestHost.WithUowAsync(sp, async uow =>
        {
            var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonQ>(uow);

            // Weder DB-Spalte noch CLR-Property
            var q = Query.WhereEquals("does_not_exist", 1);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => repo.QueryAsync(q));
            Assert.Contains("Unknown column/property", ex.Message);
        });
    }

    [Fact]
    public async Task Comparison_With_Null_Throws()
    {
        using var sp = TestHost.CreateProvider(out _);
        await SeedAsync(sp);

        await TestHost.WithUowAsync(sp, async uow =>
        {
            var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonQ>(uow);

            // > NULL ist nicht erlaubt in unserer API (führt sonst in SQL zu UNKNOWN)

            var ex = Assert.Throws<ArgumentException>(() =>
            {
                _ = Query.WhereGreater("age", null!);
            });

            Assert.Contains("do not accept null", ex.Message);
        });
    }
    [Fact]
    public async Task AndComparison_With_Null_Throws()
    {
        using var sp = TestHost.CreateProvider(out _);
        await SeedAsync(sp);

        await TestHost.WithUowAsync(sp, async uow =>
        {
            var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonQ>(uow);

            // > NULL ist nicht erlaubt in unserer API (führt sonst in SQL zu UNKNOWN)

            var ex = Assert.Throws<ArgumentException>(() =>
            {
                _ = Query.WhereEquals("age", 10)
                        .AndGreater("age", null!);
            });

            Assert.Contains("do not accept null", ex.Message);
        });
    }

    [Fact]
    public async Task Legacy_Fields_Still_Work()
    {
        using var sp = TestHost.CreateProvider();
        await SeedAsync(sp);

        await TestHost.WithUowAsync(sp, async uow =>
        {
            var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonQ>(uow);

            // Alte Properties nutzen (Backward-Compatibility)
            var legacy = new Query { WhereColumn = "last_name", WhereValue = "Turing" }
                         .OrderBy("first_name");

            var rows = await repo.QueryAsync(legacy);
            Assert.Single(rows);
            Assert.Equal("Alan", rows[0].FirstName);
        });
    }
}
