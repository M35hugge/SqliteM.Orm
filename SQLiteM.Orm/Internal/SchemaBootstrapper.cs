using SQLiteM.Abstractions;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteM.Orm.Internal
{
    /// <summary>
    /// Stellt Hilfsfunktionen zum initialen Erzeugen des Datenbankschemas bereit.
    /// </summary>
    /// <remarks>
    /// Diese Klasse führt DDL-Anweisungen aus, die über <see cref="ISqlBuilder"/> generiert werden.
    /// Ob die Ausführung idempotent ist (z. B. durch <c>CREATE TABLE IF NOT EXISTS</c>), hängt von der
    /// konkreten Implementierung des <see cref="ISqlBuilder"/> ab.
    /// </remarks>
    /// <seealso cref="ISqlBuilder"/>
    /// <seealso cref="IUnitOfWork"/>
    internal static class SchemaBootstrapper
    {
        /// <summary>
        /// Stellt sicher, dass die Tabelle für den Entitätstyp <typeparamref name="T"/> existiert,
        /// indem die entsprechende DDL-Anweisung ausgeführt wird.
        /// </summary>
        /// <typeparam name="T">Der Entitätstyp, dessen Tabelle erzeugt werden soll.</typeparam>
        /// <param name="uow">Die aktive <see cref="IUnitOfWork"/> (liefert Verbindung/Transaktion).</param>
        /// <param name="builder">Der <see cref="ISqlBuilder"/>, der die DDL-Anweisung erzeugt.</param>
        /// <param name="ct">Ein optionales <see cref="CancellationToken"/>.</param>
        /// <returns>Eine abgeschlossene Aufgabe, sobald die DDL ausgeführt wurde.</returns>
        /// <remarks>
        /// Es wird die von <paramref name="builder"/> erzeugte Anweisung <see cref="ISqlBuilder.BuildCreateTable(Type)"/>
        /// ausgeführt. Die Methode nutzt die bestehende Verbindung aus <paramref name="uow"/>.
        /// </remarks>
        public static async Task EnsureCreatedAsync<T>(IUnitOfWork uow, ISqlBuilder builder, CancellationToken ct = default)
        {
            var ddl = builder.BuildCreateTable(typeof(T));
            using var cmd = uow.Connection.CreateCommand();
            cmd.CommandText = ddl;

            if (cmd is DbCommand dbCmd)
            {
                await dbCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
            else
            {
                cmd.ExecuteNonQuery();
            }
        }
    }
}
