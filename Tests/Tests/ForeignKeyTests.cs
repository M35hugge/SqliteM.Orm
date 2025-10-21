using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using SQLiteM.Orm.Pub;
using System.Threading.Tasks;
using Tests.Entities;
using Tests.Helpers;
using Xunit;

namespace Tests.Tests;

public class ForeignKeyTests
{
    [Fact]
    public async Task DeletingPerson_CascadesToOrders()
    {
        using var sp = TestHost.CreateProvider(out _);

        // Arrange: Schema (Person vor Order)
        await TestHost.WithUowAsync(sp, async uow =>
        {
            var b = sp.GetRequiredService<ISqlBuilder>();
            await SQLiteMBootstrap.EnsureCreatedAsync(uow, b, default, typeof(Person), typeof(Order));
            await uow.CommitAsync();
        });

        

        // Arrange: Person + Orders anlegen
        long personId = await TestHost.WithUowAsync(sp, async uow =>
        {            
            var repoFactory = sp.GetRequiredService<IRepositoryFactory>();
            var rp = repoFactory.Create<Person>(uow);
            var ro = repoFactory.Create<Order>(uow);

            var id = await rp.InsertAsync(new Person { FirstName = "Ada", LastName = "Lovelace" });
            Assert.True(id > 0);

            await ro.InsertAsync(new Order { PersonId = id, Total = 10m, Note = "A" });
            await ro.InsertAsync(new Order { PersonId = id, Total = 20m, Note = "B" });
            return id;
        });

        // Act: Person löschen (ON DELETE CASCADE)
        await TestHost.WithUowAsync(sp, async uow =>
        {
            var repo = sp.GetRequiredService<IRepositoryFactory>().Create<Person>(uow);
            var del = await repo.DeleteAsync(personId);
            await uow.CommitAsync();
            Assert.Equal(1, del);

        });

        // Assert: Orders weg
        await TestHost.WithUowAsync(sp, async uow =>
        {
            var repo = sp.GetRequiredService<IRepositoryFactory>().Create<Order>(uow);
            var remaining = await repo.FindAllAsync();
            Assert.Empty(remaining);
        });
    }
    [Fact]
    public async Task InsertingOrder_WithMissingPerson_ThrowsForeignKey()
    {
        using var sp = TestHost.CreateProvider(out _);

        // Arrange: Schema (Person vor Order)
        await TestHost.WithUowAsync(sp, async uow =>
        {
            var b = sp.GetRequiredService<ISqlBuilder>();
            await SQLiteMBootstrap.EnsureCreatedAsync(uow, b, default, typeof(Person), typeof(Order));
            await uow.CommitAsync();
        });



        var ex = await Assert.ThrowsAsync<SqliteException>(async () =>
        {
            await TestHost.WithUowAsync(sp, async uow =>
            {
                var repoFactory = sp.GetRequiredService<IRepositoryFactory>();
                var rp = repoFactory.Create<Person>(uow);
                var ro = repoFactory.Create<Order>(uow);

                await ro.InsertAsync(new Order { PersonId = 999, Total = 10m, Note = "A" });
                await ro.InsertAsync(new Order { PersonId = 999, Total = 20m, Note = "B" });
            });
        });

        // Optional: Fehlercodes prüfen
        Assert.Equal(19, ex.SqliteErrorCode);         // SQLITE_CONSTRAINT
        Assert.Equal(787, ex.SqliteExtendedErrorCode); // SQLITE_CONSTRAINT_FOREIGNKEY
    }
}
