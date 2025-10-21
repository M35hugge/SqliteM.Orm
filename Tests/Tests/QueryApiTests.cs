using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using SQLiteM.Orm.Pub;
using System.Linq;
using System.Threading.Tasks;
using Tests.Entities;
using Tests.Helpers;
using Xunit;

namespace Tests.Tests;

public class QueryApiTests
{
    private static readonly string[] expected = new[] { "Ada", "Alan", "Grace" };

    [Fact]
    public async Task Query_FindAll_And_Filter_OrderBy()
    {
        using var sp = TestHost.CreateProvider(out _);

        await TestHost.WithUowAsync(sp, async uow =>
        {
            var b = sp.GetRequiredService<ISqlBuilder>();
            await SQLiteMBootstrap.EnsureCreatedAsync<Person>(uow, b);
            await uow.CommitAsync();
        });

        await TestHost.WithUowAsync(sp, async uow =>
        {
            var repo = sp.GetRequiredService<IRepositoryFactory>().Create<Person>(uow);

            await repo.InsertAsync(new Person { FirstName = "Ada", LastName = "Lovelace" });
            await repo.InsertAsync(new Person { FirstName = "Alan", LastName = "Turing" });
            await repo.InsertAsync(new Person { FirstName = "Grace", LastName = "Hopper" });
            await uow.CommitAsync();
        });

        await TestHost.WithUowAsync(sp, async uow =>
        {
            var repo = sp.GetRequiredService<IRepositoryFactory>().Create<Person>(uow);

            var all = await repo.FindAllAsync();

            Assert.Equal(3, all.Count);

            var filtered = await repo.QueryAsync(Query.WhereEquals("LastName", "Turing"));
            Assert.Single(filtered);
            Assert.Equal("Alan", filtered[0].FirstName);

            var ordered = await repo.QueryAsync(new Query().OrderBy("FirstName"));
            Assert.Equal(expected, ordered.Select(p => p.FirstName).ToArray());
        });
    }
}
