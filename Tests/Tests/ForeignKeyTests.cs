using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using SQLiteM.Orm;
using SQLiteM.Orm.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tests.Entities;
using Tests.Helpers;
using Xunit;

namespace Tests.Tests
{
    public class ForeignKeyTests
    {
        [Fact]
        public async Task DeletingPerson_CascadesToOrders()
        {
            using var sp = TestHost.CreateProvider(out _);

            await TestHost.WithUowAsync(sp, async uow =>
            {
                var b = sp.GetRequiredService<ISqlBuilder>();
                await SQLiteMBootstrap.EnsureCreatedAsync<Person>(uow, b);
                await SQLiteMBootstrap.EnsureCreatedAsync<Order>(uow, b);
                await uow.CommitAsync();
            });

            long personId=0;

            await TestHost.WithUowAsync(sp, async uow =>
            {
                var rp = sp.GetRequiredService<IRepositoryFactory>().Create<Person>(uow);
                var ro = sp.GetRequiredService<IRepositoryFactory>().Create<Order>(uow);

                personId = await rp.InsertAsync(new Person { FirstName = "Ada", LastName = "Lovelace" });
                Assert.True(personId > 0);
                await ro.InsertAsync(new Order { PersonId = personId, Total = 10m, Note = "A" });
                await ro.InsertAsync(new Order { PersonId = personId, Total = 20m, Note = "B" });
                await uow.CommitAsync();
            });

            await TestHost.WithUowAsync(sp, async uow =>
            {
                var rp = sp.GetRequiredService<IRepositoryFactory>().Create<Person>(uow);
                var ro = sp.GetRequiredService<IRepositoryFactory>().Create<Order>(uow);

                var del = await rp.DeleteAsync(personId);
                Assert.Equal(1, del);
                await uow.CommitAsync();

            });
            await TestHost.WithUowAsync(sp, async uow =>
            {
                var ro = sp.GetRequiredService<IRepositoryFactory>().Create<Order>(uow);
                var remaining = await ro.FindAllAsync();
                Assert.Empty(remaining);

            });
        }
    }
}
