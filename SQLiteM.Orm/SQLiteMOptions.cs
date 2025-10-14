using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteM.Orm
{
    public sealed class SQLiteMOptions
    {
        public string ConnectionString { get; set; } = "Data Source =:memory:";
    }
}
