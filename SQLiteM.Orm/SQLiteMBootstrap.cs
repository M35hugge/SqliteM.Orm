using System;
using System.Threading;
using System.Threading.Tasks;
using SQLiteM.Abstractions;

namespace SQLiteM.Orm
{
    /// <summary>
    /// Öffentliche Fassade für Schema-Operationen (DDL).
    /// </summary>
    /// <remarks>
    /// Diese statische Klasse kapselt Aufrufe an den internen
    /// <see cref="Internal.SchemaBootstrapper"/> und stellt bequeme Methoden bereit,
    /// um Tabellen für Entitätstypen zu erstellen.
    /// Die Methoden prüfen, ob die Tabelle bereits existiert, und führen
    /// entsprechende <c>CREATE TABLE IF NOT EXISTS</c>-Anweisungen aus.
    /// </remarks>
    /// <seealso cref="Internal.SchemaBootstrapper"/>
    /// <seealso cref="ISqlBuilder"/>
    /// <seealso cref="IUnitOfWork"/>
    /// <seealso cref="IUnitOfWorkFactory"/>
    public static class SQLiteMBootstrap
    {
        /// <summary>
        /// Erzeugt (falls nicht vorhanden) die Tabelle für den angegebenen Entitätstyp
        /// innerhalb einer bestehenden <see cref="IUnitOfWork"/>.
        /// </summary>
        /// <typeparam name="T">Der Entitätstyp, für den die Tabelle erzeugt werden soll.</typeparam>
        /// <param name="uow">Die aktive <see cref="IUnitOfWork"/> (offene Verbindung + Transaktion).</param>
        /// <param name="builder">Der <see cref="ISqlBuilder"/>, der die DDL-Anweisung generiert.</param>
        /// <param name="ct">Ein optionales <see cref="CancellationToken"/>.</param>
        /// <returns>Eine asynchrone Aufgabe, die abgeschlossen wird, sobald die Tabelle erzeugt wurde.</returns>
        /// <example>
        /// <code language="csharp">
        /// await SQLiteMBootstrap.EnsureCreatedAsync&lt;User&gt;(uow, sqlBuilder);
        /// </code>
        /// </example>
        public static Task EnsureCreatedAsync<T>(
            IUnitOfWork uow,
            ISqlBuilder builder,
            CancellationToken ct = default)
            => Internal.SchemaBootstrapper.EnsureCreatedAsync<T>(uow, builder, ct);

        /// <summary>
        /// Erzeugt (falls nicht vorhanden) die Tabelle für den angegebenen Entitätstyp.
        /// </summary>
        /// <typeparam name="T">Der Entitätstyp, für den die Tabelle erzeugt werden soll.</typeparam>
        /// <param name="uowFactory">Die <see cref="IUnitOfWorkFactory"/> zum Erstellen einer Arbeitseinheit.</param>
        /// <param name="builder">Der <see cref="ISqlBuilder"/>, der die DDL-Anweisung generiert.</param>
        /// <param name="ct">Ein optionales <see cref="CancellationToken"/>.</param>
        /// <returns>Eine asynchrone Aufgabe, die abgeschlossen wird, sobald die Tabelle erzeugt wurde.</returns>
        /// <remarks>
        /// Diese Überladung öffnet intern eine neue <see cref="IUnitOfWork"/>, führt die
        /// DDL-Anweisung aus und bestätigt anschließend die Transaktion automatisch.
        /// </remarks>
        /// <example>
        /// <code language="csharp">
        /// await SQLiteMBootstrap.EnsureCreatedAsync&lt;User&gt;(uowFactory, sqlBuilder);
        /// </code>
        /// </example>
        public static async Task EnsureCreatedAsync<T>(
            IUnitOfWorkFactory uowFactory,
            ISqlBuilder builder,
            CancellationToken ct = default)
        {
            await using var uow = await uowFactory.CreateAsync(ct).ConfigureAwait(false);
            await Internal.SchemaBootstrapper.EnsureCreatedAsync<T>(uow, builder, ct).ConfigureAwait(false);
            await uow.CommitAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Erzeugt (falls nicht vorhanden) Tabellen für die angegebenen Entitätstypen in exakt dieser Reihenfolge.
        /// </summary>
        /// <param name="uow">Die aktive <see cref="IUnitOfWork"/>.</param>
        /// <param name="builder">Der <see cref="ISqlBuilder"/> zur Generierung der DDL-Anweisungen.</param>
        /// <param name="ct">Ein optionales <see cref="CancellationToken"/>.</param>
        /// <param name="entityTypes">Die Typen, für die Tabellen erzeugt werden sollen.</param>
        /// <returns>Eine asynchrone Aufgabe, die abgeschlossen wird, sobald alle Tabellen erzeugt wurden.</returns>
        /// <remarks>
        /// Die Typen werden in der angegebenen Reihenfolge verarbeitet.
        /// Dies ist insbesondere dann relevant, wenn Fremdschlüsselbeziehungen eine
        /// bestimmte Erzeugungsreihenfolge erfordern (zuerst Principals, dann Dependents).
        /// </remarks>
        /// <example>
        /// <code language="csharp">
        /// await SQLiteMBootstrap.EnsureCreatedAsync(
        ///     uow,
        ///     sqlBuilder,
        ///     default,
        ///     typeof(User), typeof(Post), typeof(Comment));
        /// </code>
        /// </example>
        public static async Task EnsureCreatedAsync(
            IUnitOfWork uow,
            ISqlBuilder builder,
            CancellationToken ct = default,
            params Type[] entityTypes)
        {
            if (entityTypes is null || entityTypes.Length == 0) return;

            foreach (var t in entityTypes)
            {
                var method = typeof(Internal.SchemaBootstrapper)
                    .GetMethod(nameof(Internal.SchemaBootstrapper.EnsureCreatedAsync))!
                    .MakeGenericMethod(t);

                var task = (Task)method.Invoke(null, new object?[] { uow, builder, ct })!;
                await task.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Erzeugt (falls nicht vorhanden) Tabellen für die angegebenen Entitätstypen in exakt dieser Reihenfolge.
        /// </summary>
        /// <param name="uowFactory">Die <see cref="IUnitOfWorkFactory"/> zum Erstellen einer Arbeitseinheit.</param>
        /// <param name="builder">Der <see cref="ISqlBuilder"/> zur Generierung der DDL-Anweisungen.</param>
        /// <param name="ct">Ein optionales <see cref="CancellationToken"/>.</param>
        /// <param name="entityTypes">Die Typen, für die Tabellen erzeugt werden sollen.</param>
        /// <returns>Eine asynchrone Aufgabe, die abgeschlossen wird, sobald alle Tabellen erzeugt wurden.</returns>
        /// <remarks>
        /// Diese Überladung öffnet intern eine neue <see cref="IUnitOfWork"/>, erzeugt alle angegebenen Tabellen
        /// in der gewünschten Reihenfolge und bestätigt die Transaktion am Ende.
        /// </remarks>
        /// <example>
        /// <code language="csharp">
        /// await SQLiteMBootstrap.EnsureCreatedAsync(
        ///     uowFactory,
        ///     sqlBuilder,
        ///     default,
        ///     typeof(User), typeof(Post), typeof(Comment));
        /// </code>
        /// </example>
        public static async Task EnsureCreatedAsync(
            IUnitOfWorkFactory uowFactory,
            ISqlBuilder builder,
            CancellationToken ct = default,
            params Type[] entityTypes)
        {
            await using var uow = await uowFactory.CreateAsync(ct).ConfigureAwait(false);
            await EnsureCreatedAsync(uow, builder, ct, entityTypes).ConfigureAwait(false);
            await uow.CommitAsync(ct).ConfigureAwait(false);
        }
    }
}
