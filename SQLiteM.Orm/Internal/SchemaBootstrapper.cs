using SQLiteM.Abstractions;
using System.Data.Common;

namespace SQLiteM.Orm.Internal;

/// <summary>
/// Stellt Hilfsfunktionen zum initialen Erzeugen des Datenbankschemas bereit.
/// </summary>
/// <remarks>
/// Diese Klasse führt DDL-Anweisungen aus, die über <see cref="ISqlBuilder"/> generiert werden.
/// Die Idempotenz (z. B. via <c>CREATE TABLE IF NOT EXISTS</c>) liegt in der Verantwortung der
/// konkreten <see cref="ISqlBuilder"/>-Implementierung.
/// Zusätzlich können von <see cref="ISqlBuilder.BuildCreateIndexes(Type)"/> bereitgestellte
/// Index-Anweisungen ausgeführt werden.
/// </remarks>
/// <seealso cref="ISqlBuilder"/>
/// <seealso cref="IUnitOfWork"/>
internal static class SchemaBootstrapper
{
    /// <summary>
    /// Stellt sicher, dass die Tabelle für den Entitätstyp <typeparamref name="T"/> existiert,
    /// indem die entsprechende DDL-Anweisung innerhalb der aktuellen Transaktion ausgeführt wird.
    /// </summary>
    /// <typeparam name="T">Der Entitätstyp, dessen Tabelle erzeugt werden soll.</typeparam>
    /// <param name="uow">Die aktive <see cref="IUnitOfWork"/> (liefert Verbindung und Transaktion).</param>
    /// <param name="builder">Der <see cref="ISqlBuilder"/>, der die DDL-Anweisung erzeugt.</param>
    /// <param name="ct">Optionaler <see cref="CancellationToken"/>.</param>
    /// <returns>Eine Aufgabe, die abgeschlossen wird, sobald die DDL ausgeführt wurde.</returns>
    /// <exception cref="ArgumentNullException">Wenn <paramref name="uow"/> oder <paramref name="builder"/> null ist.</exception>
    /// <exception cref="InvalidOperationException">
    /// Wenn <see cref="IUnitOfWork.Connection"/> oder <see cref="IUnitOfWork.Transaction"/> null ist.
    /// </exception>
    /// <remarks>
    /// Es wird die von <paramref name="builder"/> erzeugte Anweisung <see cref="ISqlBuilder.BuildCreateTable(Type)"/>
    /// ausgeführt. Die Ausführung erfolgt stets in der aktuellen Transaktion (<see cref="IUnitOfWork.Transaction"/>).
    /// </remarks>
    public static async Task EnsureCreatedAsync<T>(IUnitOfWork uow, ISqlBuilder builder, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(uow);
        ArgumentNullException.ThrowIfNull(builder);

        if (uow.Connection is null) throw new InvalidOperationException("UnitOfWork.Connection is null, Ensure the UnitOfWork is properly created and not disposed");
        if (uow.Transaction is null) throw new InvalidOperationException("UnitOfWork.Transaction is null, Ensure the UnitOfWork is properly created and not disposed");

        // Tabelle erzeugen
        var ddl = builder.BuildCreateTable(typeof(T));

        using (var cmd = uow.Connection.CreateCommand())
        {
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

        // Indizes erzeugen
        var indexSql = builder.BuildCreateIndexes(typeof(T));
        foreach (var sql in indexSql)
        {
            using var cmd = uow.Connection.CreateCommand();
            cmd.CommandText = sql;
            if (cmd is DbCommand db)
                await db.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            else
                cmd.ExecuteNonQuery();
        }
    }
}
