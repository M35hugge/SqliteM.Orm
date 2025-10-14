using SQLiteM.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteM.Orm;

public sealed class SqliteDialect : ISqlDialect
{
    public string ParamerterPrefix => "@";

    public string QuoteIdentifier(string name) => $"\"{name}\"";
    public static string ParameterPrefix => "@";
    

}
