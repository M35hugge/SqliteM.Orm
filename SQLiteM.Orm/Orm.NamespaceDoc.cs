using SQLiteM.Orm.Pub;
using SQLiteM.Orm.Internal;
using System.Runtime.CompilerServices;

namespace SQLiteM.Orm
{
    /// <summary>
    /// Konkrete Implementierungen des SQLiteM-ORM sowie DI-Integration und Bootstrap-Utilities.
    /// </summary>
    /// <remarks>
    /// Enthält u. a.:
    /// <list type="bullet">
    /// <item><description>Einfache Clientabstraktion mit Crud-Operationen: <see cref="SQLiteMClient"/>, <see cref="SQLiteMClient"/>.</description></item>
    /// 
    /// <item><description>Dialekt- und SQL-Generierung: <see cref="SqliteDialect"/>, <see cref="SQLiteM.Orm.Internal.SqlBuilder"/>.</description></item>
    /// <item><description>Mapping und Repositories: <see cref="ReflectionEntityMapper"/>, <see cref="SQLiteM.Orm.Internal.Repository{T}"/>.</description></item>
    /// <item><description>Unit-of-Work und Verbindungen: <see cref="SQLiteM.Orm.Internal.UnitOfWork"/>, <see cref="Internal.SqliteConnectionFactory"/>.</description></item>
    /// <item><description>Bootstrapping und DI: <see cref="SQLiteMBootstrap"/>, <see cref="ServiceCollectionExtensions"/>, <see cref="SQLiteMOptions"/>.</description></item>
    /// </list>
    /// Registrierung im DI-Container:
    /// <code language="csharp">
    /// services.AddSQLiteM(opt =&gt; opt.ConnectionString = "Data Source=app.db");
    /// </code>
    /// </remarks>
    [CompilerGenerated]
    public static class NamespaceDoc { }
}
