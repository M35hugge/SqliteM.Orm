using SQLiteM.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteM.Orm.Internal
{
    /// <summary>
    /// Implementierung des <see cref="ISqlDialect"/> für SQLite.
    /// </summary>
    /// <remarks>
    /// Dieser Dialekt definiert SQLite-spezifische Formatierungsregeln für
    /// Parameter und Bezeichner:
    /// <list type="bullet">
    /// <item><description>Parameter werden mit dem Präfix <c>@</c> angegeben.</description></item>
    /// <item><description>Datenbank- und Spaltennamen werden mit doppelten Anführungszeichen (<c>"</c>) maskiert.</description></item>
    /// </list>
    /// Diese Klasse wird üblicherweise als Singleton im DI-Container registriert.
    /// </remarks>
    /// <seealso cref="ISqlDialect"/>
    public sealed class SqliteDialect : ISqlDialect
    {
        /// <summary>
        /// Gibt das Präfix für SQL-Parameter zurück.
        /// </summary>
        /// <value>Immer <c>"@"</c>, entsprechend der SQLite-Syntax.</value>
        public string ParameterPrefix => "@";

        /// <summary>
        /// Maskiert einen SQL-Bezeichner (z. B. Tabellen- oder Spaltennamen)
        /// gemäß der SQLite-Syntax.
        /// </summary>
        /// <param name="name">Der zu maskierende Bezeichner.</param>
        /// <returns>Den maskierten Bezeichner in doppelten Anführungszeichen.</returns>
        /// <example>
        /// <code language="csharp">
        /// var dialect = new SqliteDialect();
        /// var column = dialect.QuoteIdentifier("User");
        /// // Ergebnis: "User"
        /// </code>
        /// </example>
        /// <exception cref="ArgumentNullException">
        /// Wenn <paramref name="name"/> <see langword="null"/> ist.
        /// </exception>
        public string QuoteIdentifier(string name)
        {
            ArgumentNullException.ThrowIfNull(name);
            return $"\"{name}\"";
        }
    }
}
