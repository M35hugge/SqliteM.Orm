using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteM.Abstractions
{
    /// <summary>
    /// Markiert eine Klasse als Datenbanktabelle.
    /// </summary>
    /// <remarks>
    /// Wenn <see cref="Name"/> nicht gesetzt ist, wird der Tabellenname
    /// per <c>INameTranslator.Table(typeof(T).Name)</c> aus dem CLR-Typnamen abgeleitet
    /// (z. B. <c>Person</c> → <c>person</c> bzw. <c>person</c>/<c>persons</c>, je nach Translator).
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// [Table("users")]
    /// public class User { ... }
    ///
    /// // Oder ohne Parameter → Name wird vom Translator aus dem Typnamen abgeleitet
    /// [Table]
    /// public class Order { ... }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TableAttribute : Attribute
    {
        /// <summary>Physischer Tabellenname in der Datenbank (optional).</summary>
        public string? Name { get; }

        /// <summary>Parameterlos: Name wird über den <c>INameTranslator</c> aus dem Typnamen bestimmt.</summary>
        public TableAttribute() { }

        /// <summary>Expliziter Tabellenname.</summary>
        public TableAttribute(string name) => Name = name;
    }

    /// <summary>
    /// Markiert eine Eigenschaft als Datenbankspalte.
    /// </summary>
    /// <remarks>
    /// Wenn <see cref="Name"/> nicht gesetzt ist, wird der Spaltenname
    /// per <c>INameTranslator.Column(property.Name)</c> aus dem CLR-Propertynamen abgeleitet
    /// (z. B. <c>FirstName</c> → <c>first_name</c>).
    /// <para>
    /// <see cref="IsUniqueColumn"/> erzeugt einen <b>Spalten-UNIQUE-Constraint</b> in der
    /// <c>CREATE TABLE</c>-DDL (z. B. <c>email TEXT UNIQUE</c>). Für zusammengesetzte Eindeutigkeit
    /// über mehrere Spalten verwende <see cref="CompositeIndexAttribute"/> mit <c>IsUnique = true</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// [Column("email", IsNullable = false, Length = 255, IsUniqueColumn = true)]
    /// public string Email { get; set; } = string.Empty;
    ///
    /// // Ohne Name → Spaltenname wird aus dem Propertynamen abgeleitet
    /// [Column]
    /// public string FirstName { get; set; } = string.Empty;
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public sealed class ColumnAttribute : Attribute
    {
        /// <summary>Spaltenname in der Tabelle (optional).</summary>
        public string? Name { get; }

        /// <summary>Gibt an, ob <c>NULL</c> erlaubt ist. Standard: <c>true</c>.</summary>
        public bool IsNullable { get; set; } = true;

        /// <summary>Maximale Länge für Textspalten (<see cref="string"/>). 0 = kein Limit (nur deklarativ in SQLite).</summary>
        public int Length { get; set; } = 0;

        /// <summary>
        /// Erzwingt Eindeutigkeit über <b>diese einzelne</b> Spalte (Spalten-Constraint <c>UNIQUE</c> in der Tabelle).
        /// </summary>
        public bool IsUniqueColumn { get; set; } = false;

        /// <summary>Parameterlos: Name wird über den <c>INameTranslator</c> bestimmt.</summary>
        public ColumnAttribute() { }

        /// <summary>Expliziter Spaltenname (optional: Direktvorgabe von <see cref="IsNullable"/>).</summary>
        public ColumnAttribute(string name, bool isNullable = true) => (Name, IsNullable) = (name, isNullable);
    }

    /// <summary>
    /// Markiert eine Eigenschaft als Primärschlüssel der Tabelle.
    /// </summary>
    /// <remarks>
    /// In der Regel genau ein Property pro Entität. In Kombination mit <see cref="AutoIncrementAttribute"/>
    /// wird in SQLite <c>INTEGER PRIMARY KEY AUTOINCREMENT</c> erzeugt.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public sealed class PrimaryKeyAttribute : Attribute { }

    /// <summary>
    /// Markiert eine Eigenschaft als (möglicherweise eindeutigen) Index der Tabelle.
    /// </summary>
    /// <remarks>
    /// Erzeugt per DDL einen separaten Index via <c>CREATE [UNIQUE] INDEX IF NOT EXISTS ...</c>.
    /// Für zusammengesetzte Indizes über mehrere Spalten verwende <see cref="CompositeIndexAttribute"/>.
    /// <para>
    /// <see cref="IsUnique"/> erzeugt einen <b>UNIQUE-Index</b>. Das unterscheidet sich von
    /// <see cref="ColumnAttribute.IsUniqueColumn"/>, welches einen <b>Spalten-UNIQUE-Constraint</b>
    /// innerhalb der Tabelle erzeugt. Funktional ähnlich, aber unterschiedliche Stelle der DDL.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// [Index] // nicht-unique Einzelspaltenindex
    /// public string LastName { get; set; } = string.Empty;
    ///
    /// [Index(IsUnique = true)] // eindeutiger Einzelspaltenindex
    /// public string Email { get; set; } = string.Empty;
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public sealed class IndexAttribute : Attribute
    {
        /// <summary>
        /// Optionaler Indexname; wenn nicht gesetzt, generiert der Builder einen Namen
        /// (z. B. <c>ix_{tabelle}_{spalte}</c>).
        /// </summary>
        public string? Name { get; set; }

        /// <summary>Erzeugt einen eindeutigen Index (<c>UNIQUE INDEX</c>).</summary>
        public bool IsUnique { get; set; } = false;
    }

    /// <summary>
    /// Markiert die Klasse mit einem (möglicherweise eindeutigen) <b>zusammengesetzten Index</b>
    /// über mehrere Spalten.
    /// </summary>
    /// <remarks>
    /// Die im Konstruktor übergebenen <see cref="Columns"/> sind <b>CLR-Propertynamen</b>.
    /// Der Mapper übersetzt sie zu DB-Spaltennamen (z. B. via Snake-Case-Translator).
    /// Wird <see cref="Name"/> nicht gesetzt, generiert der Builder einen konsistenten Namen,
    /// z. B. <c>ix_{tabelle}_{spalte1}_{spalte2}</c>.
    /// <para>
    /// Für Eindeutigkeit über mehrere Spalten setze <see cref="IsUnique"/> auf <c>true</c>
    /// (DDL: <c>CREATE UNIQUE INDEX ...</c>).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// [CompositeIndex(nameof(FirstName), nameof(LastName), IsUnique = true)]
    /// public class User { ... }
    /// </code>
    /// </example>  
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class CompositeIndexAttribute : Attribute
    {
        /// <summary>Initialisiert einen zusammengesetzten Index über die angegebenen CLR-Propertynamen.</summary>
        public CompositeIndexAttribute(params string[] columns) => Columns = columns ?? Array.Empty<string>();

        /// <summary>CLR-Propertynamen, die im Index enthalten sind (werden zu DB-Spaltennamen übersetzt).</summary>
        public string[] Columns { get; }

        /// <summary>Optionaler Indexname (sonst generiert der Builder einen Namen).</summary>
        public string? Name { get; set; }

        /// <summary>Erzeugt einen eindeutigen Index (<c>UNIQUE</c>).</summary>
        public bool IsUnique { get; set; } = false;
    }


    /// <summary>
    /// Markiert eine Eigenschaft als Auto-Increment-Spalte.
    /// </summary>
    /// <remarks>
    /// Üblicherweise gemeinsam mit <see cref="PrimaryKeyAttribute"/> auf einer <c>INTEGER</c>-Spalte.
    /// In SQLite ergibt sich daraus typischerweise <c>INTEGER PRIMARY KEY AUTOINCREMENT</c>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public sealed class AutoIncrementAttribute : Attribute { }


    /// <summary>
    /// Markiert eine Eigenschaft als Fremdschlüssel (Referenz auf eine Principal-Tabelle).
    /// </summary>
    /// <remarks>
    /// Die Fremdschlüsseldefinition wird beim <c>CREATE TABLE</c> berücksichtigt.
    /// Wenn nur der <see cref="PrincipalEntity"/> angegeben wird, gilt per Konvention
    /// <see cref="PrincipalColumn"/> = <c>"Id"</c> (PK-Spalte).
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// // Verweist auf User.Id, setzt Cascade-Delete
    /// [ForeignKey(typeof(User), nameof(User.Id), OnDelete = OnDeleteAction.Cascade)]
    /// [Column("user_id")]
    /// public long UserId { get; set; }
    ///
    /// // Nur Principal-Typ → Spaltenname "Id" wird angenommen
    /// [ForeignKey(typeof(User))]
    /// [Column("user_id")]
    /// public long UserId { get; set; }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public sealed class ForeignKeyAttribute : Attribute
    {
        /// <summary>Typ der referenzierten Entität (Principal-Tabelle).</summary>
        public Type PrincipalEntity { get; }

        /// <summary>Name der referenzierten Spalte in der Principal-Tabelle (z. B. <c>"Id"</c>).</summary>
        public string PrincipalColumn { get; }

        /// <summary>Aktion bei Löschung der Principal-Zeile (z. B. <c>CASCADE</c>).</summary>
        public OnDeleteAction OnDelete { get; set; } = OnDeleteAction.NoAction;

        /// <summary>
        /// Verwendet per Konvention <c>"Id"</c> als <see cref="PrincipalColumn"/>.
        /// </summary>
        public ForeignKeyAttribute(Type principalEntity)
        {
            PrincipalEntity = principalEntity ?? throw new ArgumentNullException(nameof(principalEntity));
            PrincipalColumn = "Id"; // Konvention: PK-Spalte "Id"
        }

        /// <summary>Explizite Angabe der referenzierten Spalte.</summary>
        public ForeignKeyAttribute(Type principalEntity, string principalColumn)
        {
            PrincipalEntity = principalEntity ?? throw new ArgumentNullException(nameof(principalEntity));
            PrincipalColumn = principalColumn ?? throw new ArgumentNullException(nameof(principalColumn));
        }
    }


    /// <summary>
    /// Schließt eine Eigenschaft vom Mapping aus (z. B. Navigation oder berechnetes Feld).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public sealed class IgnoreAttribute : Attribute { }


}
