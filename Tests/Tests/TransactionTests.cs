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
    public class TransactionTests
    {
        private static async Task EnsureSchemaAsync(ServiceProvider sp)
        {
            await TestHost.WithUowAsync(sp, async uow =>
            {
                var b = sp.GetRequiredService<ISqlBuilder>();
                await SQLiteMBootstrap.EnsureCreatedAsync<PersonTx>(uow, b);
                await uow.CommitAsync();
            });
        }

        [Fact]
        public async Task Commit_Persists_Across_Scopes()
        {
            using var sp = TestHost.CreateProvider();
            await EnsureSchemaAsync(sp);

            // Insert + Commit
            await TestHost.WithUowAsync(sp, async uow =>
            {
                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonTx>(uow);
                var id = await repo.InsertAsync(new PersonTx { Name = "Ada" });
                Assert.True(id > 0);
                await uow.CommitAsync();
            });

            // Neuer Scope: Datensatz ist da
            await TestHost.WithUowAsync(sp, async uow =>
            {
                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonTx>(uow);
                var all = await repo.FindAllAsync();
                Assert.Contains(all, p => p.Name == "Ada");
            });
        }

        [Fact]
        public async Task Rollback_Discards_Changes()
        {
            using var sp = TestHost.CreateProvider();
            await EnsureSchemaAsync(sp);

            // Insert ohne Commit → Rollback
            await TestHost.WithUowAsync(sp, async uow =>
            {
                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonTx>(uow);
                var id = await repo.InsertAsync(new PersonTx { Name = "Grace" });
                Assert.True(id > 0);
                // kein Commit auf diesem Scope
                await uow.RollbackAsync();
            });

            // Neuer Scope: Datensatz ist NICHT da
            await TestHost.WithUowAsync(sp, async uow =>
            {
                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonTx>(uow);
                var all = await repo.FindAllAsync();
                Assert.DoesNotContain(all, p => p.Name == "Grace");
            });
        }

        [Fact]
        public async Task Dispose_Without_Commit_RollsBack()
        {
            using var sp = TestHost.CreateProvider();
            await EnsureSchemaAsync(sp);

            // eigener Scope ohne Commit, nur Dispose
            await TestHost.WithUowAsync(sp, async uow =>
            {
                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonTx>(uow);
                var id = await repo.InsertAsync(new PersonTx { Name = "Alan" });
                Assert.True(id > 0);
                // kein Commit/kein Rollback explizit → Dispose sollte Rollback auslösen
            });

            // Neuer Scope: Datensatz ist NICHT da
            await TestHost.WithUowAsync(sp, async uow =>
            {
                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonTx>(uow);
                var all = await repo.FindAllAsync();
                Assert.DoesNotContain(all, p => p.Name == "Alan");
            });
        }

        [Fact]
        public async Task Using_Uow_After_Commit_Throws()
        {
            using var sp = TestHost.CreateProvider();
            await EnsureSchemaAsync(sp);

            // Hinweis: Dieser Test erwartet, dass nach Commit eine weitere Nutzung der UoW fehlschlägt.
            // Falls er aktuell GRÜN ist, fehlt in UnitOfWork/Repository eine Sperre.
            // Empfohlene Fixes:
            // - In UnitOfWork.CommitAsync: nach Commit Transaction.Dispose(); Transaction = null;
            // - In UnitOfWork.DisposeAsync: IMMER Connection/Transaction disposen (auch wenn _completed == true)
            // - In Repository.EnsureConnectionAndTransaction(): auf _uow.Transaction == null prüfen (wirft)
            await TestHost.WithUowAsync(sp, async uow =>
            {
                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonTx>(uow);

                var id = await repo.InsertAsync(new PersonTx { Name = "Barbara" });
                Assert.True(id > 0);

                await uow.CommitAsync();

                // erneute Nutzung im selben Scope sollte eine Exception auslösen
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await repo.FindAllAsync();
                });
            });
        }
    }
}
