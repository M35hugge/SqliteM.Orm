
using SQLiteM.Orm;
using SQLiteM.Orm.Impl;
using SQLiteM.Orm.Internal;
using Tests.Entities;
using Xunit;

namespace Tests.Tests
{
    public class SqlBuilderTests
    {
        [Fact]
        public void CreateTable_Person_HasNoInnerSemicolons()
        {


            var ddl = BuildPerson();

            Assert.DoesNotContain("););", ddl);
            Assert.Matches(@";\s*$", ddl);
            Assert.DoesNotMatch(@".+;.+;", ddl);

        }

        [Fact]
        public void CreateTable_Order_DoesContainForeignKey()
        {
            var ddl = BuildOrder();

            Assert.Contains("FOREIGN KEY", ddl);

            Assert.Contains("FOREIGN KEY", ddl);
            Assert.DoesNotContain(")REFERENCES", ddl);
            Assert.Contains("REFERENCES \"persons\" (\"Id\")", ddl); // abhängig von deinen Table/Column-Namen
            Assert.Contains("ON DELETE CASCADE", ddl);
            Assert.Matches(@";\s*$", ddl);
        }
        public static string BuildOrder()
        {
            var mapper = new ReflectionEntityMapper();
            var dialect = new SqliteDialect();
            var sql = new SqlBuilder(mapper, dialect);
            var ddl = sql.BuildCreateTable(typeof(Order));
            return ddl;
        }


        public static string BuildPerson()
        {
            var mapper = new ReflectionEntityMapper();
            var dialect = new SqliteDialect();
            var sql = new SqlBuilder(mapper, dialect);
            var ddl = sql.BuildCreateTable(typeof(Person));
            return ddl;
        }
    }
}