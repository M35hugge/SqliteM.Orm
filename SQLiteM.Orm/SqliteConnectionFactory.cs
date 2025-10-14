using Microsoft.Data.Sqlite;
using SQLiteM.Abstractions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteM.Orm
{
    public sealed class SqliteConnectionFactory(string connectionString) : IConnectionFactory
    {
        private readonly string _connectionString = connectionString;
        public IDbConnection Create() => new SqliteConnection(_connectionString);
    }
}
