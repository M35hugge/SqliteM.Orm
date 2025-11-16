using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using SQLiteM.Orm.Pub;
using System;
using System.Threading.Tasks;
using Tests.Entities;
using Tests.Helpers;
using Xunit;

namespace Tests.Tests
{
    public class RepositoryEdgeTests
    {
        private static async Task EnsureAsync<T>(ServiceProvider sp)
        {
            await TestHost.WithUowAsync(sp, async uow =>
            {
                var b = sp.GetRequiredService<ISqlBuilder>();
                await SQLiteMBootstrap.EnsureCreatedAsync<T>(uow, b);
                await uow.CommitAsync();
            });
        }

        [Fact]
        public async Task Insert_Sets_AutoIncrement_Id_And_Persists()
        {
            using var sp = TestHost.CreateProvider();
            await EnsureAsync<PersonRepo>(sp);

            int newId = await TestHost.WithUowAsync(sp, async uow =>
            {
                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonRepo>(uow);
                var p = new PersonRepo { FirstName = "Ada", LastName = "Lovelace" };
                var id = await repo.InsertAsync(p);
                Assert.True(id > 0);
                Assert.Equal(id, p.Id); // Objekt-Id gesetzt
                return id;
            });

            await TestHost.WithUowAsync(sp, async uow =>
            {
                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonRepo>(uow);
                var again = await repo.FindByIdAsync(newId);
                Assert.NotNull(again);
                Assert.Equal("Ada", again!.FirstName);
            });
        }

        [Fact]
        public async Task Update_Ignores_PK_And_Respects_Translator_For_Sets()
        {
            using var sp = TestHost.CreateProvider();
            await EnsureAsync<PersonRepo>(sp);

            var id = await TestHost.WithUowAsync(sp, async uow =>
            {
                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonRepo>(uow);
                return await repo.InsertAsync(new PersonRepo { FirstName = "Alan", LastName = "Turing" });
            });

            await TestHost.WithUowAsync(sp, async uow =>
            {
                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonRepo>(uow);
                var p = await repo.FindByIdAsync(id);
                p!.LastName = "Mathison";
                var rows = await repo.UpdateAsync(p);
                await uow.CommitAsync();
                Assert.Equal(1, rows);
            });

            await TestHost.WithUowAsync(sp, async uow =>
            {
                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonRepo>(uow);
                var p = await repo.FindByIdAsync(id);
                Assert.Equal("Mathison", p!.LastName);
            });
        }

        [Fact]
        public async Task Nullability_Is_Enforced()
        {
            using var sp = TestHost.CreateProvider();
            await EnsureAsync<PersonRepo>(sp);

            await TestHost.WithUowAsync(sp, async uow =>
            {
                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonRepo>(uow);
                // FirstName ist NOT NULL → SQLite Error 19 erwartet
                var ex = await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(async () =>
                    await repo.InsertAsync(new PersonRepo { FirstName = null!, LastName = "X" })
                );
                Assert.Contains("NOT NULL", ex.Message, StringComparison.OrdinalIgnoreCase);
            });
        }

        [Fact]
        public async Task Unique_Column_Constraint_Violates()
        {
            using var sp = TestHost.CreateProvider();
            await EnsureAsync<PersonRepo>(sp);

            await TestHost.WithUowAsync(sp, async uow =>
            {
                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonRepo>(uow);
                await repo.InsertAsync(new PersonRepo { FirstName = "G1", Email = "dup@example.com" });
                // zweiter gleicher E-Mail → UNIQUE verletzt
                var ex = await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(async () =>
                    await repo.InsertAsync(new PersonRepo { FirstName = "G2", Email = "dup@example.com" })
                );
                Assert.Contains("UNIQUE", ex.Message, StringComparison.OrdinalIgnoreCase);
            });
        }

        [Fact]
        public async Task Missing_PrimaryKey_Throws_On_Update_Delete_SelectById()
        {
            using var sp = TestHost.CreateProvider();
            await EnsureAsync<NoPkEntity>(sp);

            await TestHost.WithUowAsync(sp, async uow =>
            {
                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<NoPkEntity>(uow);

                var e = new NoPkEntity { Value = "x" };
                // Insert ohne PK ist okay (wir generieren simple INSERTs)
                var inserted = await repo.InsertAsync(e);
                await uow.CommitAsync();

                Assert.Equal(0, inserted); // kein AI-PK → 0 verabredet

                // Update/Delete/FindById müssen scheitern (kein PK-Mapping)
                await Assert.ThrowsAsync<InvalidOperationException>(() => repo.UpdateAsync(e));
                await uow.CommitAsync();

                await Assert.ThrowsAsync<InvalidOperationException>(() => repo.DeleteAsync(1));
                await uow.CommitAsync();

                await Assert.ThrowsAsync<InvalidOperationException>(() => repo.FindByIdAsync(1));
            });
        }

        [Fact]
        public async Task Query_Allows_ClrOrDb_Names_In_Filters_And_Order()
        {
            using var sp = TestHost.CreateProvider();
            await EnsureAsync<PersonRepo>(sp);

            await TestHost.WithUowAsync(sp, async uow =>
            {
                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonRepo>(uow);
                await repo.InsertAsync(new PersonRepo { FirstName = "A", LastName = "Z" });
                await repo.InsertAsync(new PersonRepo { FirstName = "B", LastName = "Y" });
                await uow.CommitAsync();
            });

            await TestHost.WithUowAsync(sp, async uow =>
            {
                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonRepo>(uow);

                // Mischung aus CLR- und DB-Spaltennamen
                var q = Query
                    .WhereEquals("LastName", "Y")       // CLR → Übersetzung zu last_name
                    .OrderBy("first_name", desc: true); // DB-Name direkt

                var rows = await repo.QueryAsync(q);
                Assert.Single(rows);
                Assert.Equal("B", rows[0].FirstName);
            });
        }
    }
}
