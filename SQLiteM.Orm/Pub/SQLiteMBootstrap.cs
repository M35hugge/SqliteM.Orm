using SQLiteM.Abstractions;
using SQLiteM.Orm.Internal;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SQLiteM.Orm.Pub
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
        /// <typeparam name="T">Der Entitätstyp.</typeparam>
        /// <param name="uow">Aktive <see cref="IUnitOfWork"/> (offene Verbindung + Transaktion).</param>
        /// <param name="builder">Der <see cref="ISqlBuilder"/> zur DDL-Generierung.</param>
        /// <param name="ct">Optionaler <see cref="CancellationToken"/>.</param>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="uow"/> oder <paramref name="builder"/> null ist.</exception>
        /// <exception cref="InvalidOperationException">
        /// Wenn <see cref="IUnitOfWork.Connection"/> oder <see cref="IUnitOfWork.Transaction"/> null ist.
        /// </exception>
        public static Task EnsureCreatedAsync<T>(IUnitOfWork uow,ISqlBuilder builder,CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(uow);
            ArgumentNullException.ThrowIfNull(builder);

            if (uow.Connection is null)
                throw new InvalidOperationException("UnitOfWork.Connection is null.Ensure UnitOfWork is properly created and not disposed.");
            if (uow.Transaction is null)
                throw new InvalidOperationException("UnitOfWork.Transaction is null.Ensure UnitOfWork is properly created and not disposed.");

            return Internal.SchemaBootstrapper.EnsureCreatedAsync<T>(uow, builder, ct);
        }

        /// <summary>
        /// Erzeugt (falls nicht vorhanden) die Tabelle für den angegebenen Entitätstyp.
        /// Öffnet intern eine neue <see cref="IUnitOfWork"/> und bestätigt am Ende automatisch.
        /// </summary>
        /// <typeparam name="T">Der Entitätstyp.</typeparam>
        /// <param name="uowFactory">Die <see cref="IUnitOfWorkFactory"/>.</param>
        /// <param name="builder">Der <see cref="ISqlBuilder"/>.</param>
        /// <param name="ct">Optionaler <see cref="CancellationToken"/>.</param>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="uowFactory"/> oder <paramref name="builder"/> null ist.</exception>
        public static async Task EnsureCreatedAsync<T>(IUnitOfWorkFactory uowFactory,ISqlBuilder builder,CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(uowFactory);
            ArgumentNullException.ThrowIfNull(builder);

            await using var uow = await uowFactory.CreateAsync(ct).ConfigureAwait(false);
            await Internal.SchemaBootstrapper.EnsureCreatedAsync<T>(uow, builder, ct).ConfigureAwait(false);
            await uow.CommitAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Erzeugt (falls nicht vorhanden) Tabellen für die angegebenen Entitätstypen in exakt dieser Reihenfolge.
        /// </summary>
        /// <param name="uow">Aktive <see cref="IUnitOfWork"/>.</param>
        /// <param name="builder">Der <see cref="ISqlBuilder"/>.</param>
        /// <param name="ct">Optionaler <see cref="CancellationToken"/>.</param>
        /// <param name="entityTypes">Die Entitätstypen (Reihenfolge wird eingehalten).</param>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="uow"/>, <paramref name="builder"/> oder <paramref name="entityTypes"/> null ist.</exception>
        /// <exception cref="InvalidOperationException">
        /// Wenn <see cref="IUnitOfWork.Connection"/> oder <see cref="IUnitOfWork.Transaction"/> null ist,
        /// oder wenn die interne Bootstrap-Methode nicht aufgerufen werden kann.
        /// </exception>
        public static async Task EnsureCreatedAsync(
            IUnitOfWork uow,
            ISqlBuilder builder,
            CancellationToken ct = default,
            params Type[] entityTypes)
        {
            ArgumentNullException.ThrowIfNull(uow);
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(entityTypes);
            if (entityTypes.Length == 0) return;


            if (uow.Connection is null)
                throw new InvalidOperationException("UnitOfWork.Connection is null.Ensure UnitOfWork is properly created and not disposed.");
            if (uow.Transaction is null)
                throw new InvalidOperationException("UnitOfWork.Transaction is null.Ensure UnitOfWork is properly created and not disposed.");

            var bootstrapType = typeof(SchemaBootstrapper);
            var generic = bootstrapType.GetMethod(
                nameof(SchemaBootstrapper.EnsureCreatedAsync),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("Schema bootstrap method not found.");

            foreach (var t in entityTypes)
            {
                if (t is null) continue;

                var method = generic.MakeGenericMethod(t);
#nullable enable
                var task = (Task)method.Invoke(null, new object?[] { uow, builder, ct })!;
                await task.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Erzeugt (falls nicht vorhanden) Tabellen für die angegebenen Entitätstypen in exakt dieser Reihenfolge.
        /// Öffnet intern eine neue <see cref="IUnitOfWork"/> und bestätigt am Ende automatisch.
        /// </summary>
        /// <param name="uowFactory">Die <see cref="IUnitOfWorkFactory"/>.</param>
        /// <param name="builder">Der <see cref="ISqlBuilder"/>.</param>
        /// <param name="ct">Optionaler <see cref="CancellationToken"/>.</param>
        /// <param name="entityTypes">Die Entitätstypen (Reihenfolge wird eingehalten).</param>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="uowFactory"/>, <paramref name="builder"/> oder <paramref name="entityTypes"/> null ist.</exception>
        public static async Task EnsureCreatedAsync(IUnitOfWorkFactory uowFactory,ISqlBuilder builder,CancellationToken ct = default,params Type[] entityTypes)
        {
            ArgumentNullException.ThrowIfNull(uowFactory);
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(entityTypes);
            if (entityTypes.Length == 0) return;

            await using var uow = await uowFactory.CreateAsync(ct).ConfigureAwait(false);
            await EnsureCreatedAsync(uow, builder, ct, entityTypes).ConfigureAwait(false);
            await uow.CommitAsync(ct).ConfigureAwait(false);
        }
    }
}
