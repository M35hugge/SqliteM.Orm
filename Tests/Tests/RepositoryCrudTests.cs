using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using SQLiteM.Orm.Pub;
using System.Threading.Tasks;
using Tests.Entities;
using Tests.Helpers;
using Xunit;

namespace Tests.Tests;

public class RepositoryCrudTests
{
    [Fact]
    public async Task Person_Crud_Roundtrip()
    {
        using var sp = TestHost.CreateProvider(out _);

        await TestHost.WithUowAsync(sp, async uow =>
        {
            var sqlBuilder = sp.GetRequiredService<ISqlBuilder>();
            await SQLiteMBootstrap.EnsureCreatedAsync<Person>(uow, sqlBuilder);
            await uow.CommitAsync();
        });

        long id;

        //Insert
        await TestHost.WithUowAsync(sp, async uow =>
        {
            var repo = sp.GetRequiredService<IRepositoryFactory>().Create<Person>(uow);
            id = await repo.InsertAsync(new Person { FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com" });
            Assert.True(id > 0);
            await uow.CommitAsync();
        });

        //Read + Update
        await TestHost.WithUowAsync(sp, async uow =>
        {
            var repo = sp.GetRequiredService<IRepositoryFactory>().Create<Person>(uow);
            var p = await repo.FindByIdAsync(1L);
            Assert.NotNull(p);
            Assert.Equal("Ada", p!.FirstName);

            p.Email = "ada.Lovelace@history.org";
            var rows = await repo.UpdateAsync(p);
            Assert.Equal(1, rows);

            await uow.CommitAsync();
        });

        await TestHost.WithUowAsync(sp, async uow =>
        {
            var repo = sp.GetRequiredService<IRepositoryFactory>().Create<Person>(uow);
            var rows = await repo.DeleteAsync(1L);
            Assert.Equal(1, rows);
            await uow.CommitAsync();
        });
    }
}
