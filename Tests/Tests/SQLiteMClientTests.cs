using Microsoft.Extensions.DependencyInjection;
using SQLiteM.Abstractions;
using SQLiteM.Orm.Pub;
using System.Linq;
using System.Threading.Tasks;
using Tests.Entities;
using Tests.Helpers;
using Xunit;

namespace Tests.Tests;


public class SQLiteMClientTests
{
    static string? dbPath;
    readonly ServiceProvider sp = TestHost.CreateProvider(out dbPath);
    readonly SQLiteMClient client = new(dbPath);

    [Fact]
    public void CreateClient()
    {
        Assert.NotNull(client);
    }
    [Fact]
    public async Task ClientEnsureCreateAsync()
    {

        await client.EnsureCreatedAsync<Person>();

    }

    [Fact]
    public async Task ClientInsertAsync()
    {
        await client.EnsureCreatedAsync<Person>();
        Person person = new() { FirstName="Bob",LastName="Uncle", Email="bob@uncle.com"};
        long id = await client.InsertAsync(person);
        Assert.True(id > 0);
        Assert.Equal(id, person.Id);
    }

    [Fact]
    public async Task ClientUpdateAsync()
    {
        await client.EnsureCreatedAsync<Person>();

        var person = new Person { FirstName = "Bob", LastName = "Uncle", Email = "bob@uncle.com" };
        var id = await client.InsertAsync(person);

        // Variante 1: dasselbe Objekt ändern
        person.FirstName = "bob";
        await client.UpdateAsync(person);

        var retour = await client.FindByIdAsync<Person>(id);
        Assert.NotNull(retour);
        Assert.Equal("bob", retour!.FirstName);
    }
    [Fact]
    public async Task ClientDeleteAsync()
    {
        await client.EnsureCreatedAsync<Person>();
        var person1 = new Person { FirstName = "Bob1", LastName = "Uncle1", Email = "bob1@uncle.com" };
        var person2 = new Person { FirstName = "Bob2", LastName = "Uncle2", Email = "bob2@uncle.com" };
        var person3 = new Person { FirstName = "Bob3", LastName = "Uncle3", Email = "bob3@uncle.com" };

        var id1 = await client.InsertAsync(person1);
        var id2 = await client.InsertAsync(person2);
        var id3 = await client.InsertAsync(person3);

        await client.DeleteAsync<Person>(id1);
        await client.DeleteAsync<Person>(id2);
        await client.DeleteAsync<Person>(id3);


        var persons= await client.FindAllAsync<Person>();
        Assert.Empty(persons);
    }

    [Fact]
    public async Task ClientQueryAsync()
    {
        await client.EnsureCreatedAsync<Person>();
        var person1 = new Person { FirstName = "Bob1", LastName = "Uncle1", Email = "bob1@uncle.com" };

        await client.InsertAsync(person1);
        var bob = await client.QueryAsync<Person>(Query.WhereEquals("FirstName", person1.FirstName));

        Assert.NotNull(bob);
        Assert.Single(bob);
        Assert.Equal(bob[0].FirstName, person1.FirstName);

    }

}
