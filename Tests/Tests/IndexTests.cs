using Microsoft.Data.Sqlite;
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
    public class IndexTests
    {
        [Fact]
        public async Task UniqueColumn_PreventsDuplicates()
        {
            using var sp = TestHost.CreateProvider(out _);

            // Schema
            await TestHost.WithUowAsync(sp, async uow =>
            {
                var b = sp.GetRequiredService<ISqlBuilder>();
                await SQLiteMBootstrap.EnsureCreatedAsync(uow, b, default, typeof(PersonWithUniqueEmail));
                await uow.CommitAsync();
            });

            // Erste Insert OK
            await TestHost.WithUowAsync(sp, async uow =>
            {
                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonWithUniqueEmail>(uow);
                var id = await repo.InsertAsync(new PersonWithUniqueEmail { FirstName = "Ada", LastName = "Lovelace", Email = "dup@example.com" });
                Assert.True(id > 0);
                await uow.CommitAsync();
            });

            // Zweite Insert mit gleicher Email → UNIQUE-Constraint verletzt
            await TestHost.WithUowAsync(sp, async uow =>
            {
                var repo = sp.GetRequiredService<IRepositoryFactory>().Create<PersonWithUniqueEmail>(uow);
                var ex = await Assert.ThrowsAsync<SqliteException>(async () =>
                {
                    await repo.InsertAsync(new PersonWithUniqueEmail { FirstName = "Alan", LastName = "Turing", Email = "dup@example.com" });
                });
                Assert.Equal(19, ex.SqliteErrorCode); // SQLITE_CONSTRAINT
            });
        }

        #region Test Entities

        [Table("test_entity")]
        public sealed class TestEntity
        {
            [PrimaryKey, AutoIncrement]
            [Column("id")]
            public long Id { get; set; }

            [Index]
            [Column("user_index")]
            public int UserIndex { get; set; }
        }
        // Spalten-UNIQUE (Constraint in CREATE TABLE)
        [Table("persons_unique")]
        public sealed class PersonWithUniqueEmail
        {
            [PrimaryKey, AutoIncrement]
            public long Id { get; set; }

            public string FirstName { get; set; } = default!;
            public string LastName { get; set; } = default!;

            // Spalten-Constraint UNIQUE
            [Column("email", IsNullable = true, Length = 255, IsUniqueColumn = true)]
            public string? Email { get; set; }
        }

        // Nicht-unique Einzelspalten-Index (CREATE INDEX ...)
        [Table("persons_ix")]
        public sealed class PersonWithIndex
        {
            [PrimaryKey, AutoIncrement]
            public long Id { get; set; }

            public string FirstName { get; set; } = default!;

            [Index] // nicht-unique
            public string LastName { get; set; } = default!;
        }

        // Composite UNIQUE Index über zwei Spalten
        [CompositeIndex(nameof(FirstName), nameof(LastName), IsUnique = true)]
        [Table("users_cix")]
        public sealed class UserWithCompositeUnique
        {
            [PrimaryKey, AutoIncrement]
            public long Id { get; set; }

            public string FirstName { get; set; } = default!;
            public string LastName { get; set; } = default!;
            public string? Email { get; set; }
        }

        #endregion
    }
}
