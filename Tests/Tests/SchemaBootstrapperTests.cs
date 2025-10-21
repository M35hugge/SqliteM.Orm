using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using SQLiteM.Orm.Pub;
using System.IO;
using System.Threading.Tasks;
using Tests.Entities;
using Tests.Helpers;
using Xunit;

namespace Tests.Tests;


public class SchemaBootstrapperTests
{

    [Fact]
    public async Task EnsureCreated_CreateBothTables()
    {
        using var sp = TestHost.CreateProvider(out var dbPath);

        await TestHost.WithUowAsync(sp, async uow =>
        {
            var sqlBuilder = sp.GetRequiredService<ISqlBuilder>();
            await SQLiteMBootstrap.EnsureCreatedAsync<Person>(uow, sqlBuilder);
            await SQLiteMBootstrap.EnsureCreatedAsync<Order>(uow, sqlBuilder);

            await uow.CommitAsync();
        });

        Assert.True(File.Exists(dbPath));
        var len = new FileInfo(dbPath).Length;
        Assert.True(len > 0);
    }
}
