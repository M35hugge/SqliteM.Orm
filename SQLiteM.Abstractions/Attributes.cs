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
    /// Der Tabellenname wird über den Konstruktor angegeben und entspricht dem
    /// physischen Tabellennamen in der Datenbank.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// [Table("Users")]
    /// public class User
    /// {
    ///     [PrimaryKey]
    ///     [AutoIncrement]
    ///     [Column("Id")]
    ///     public int Id { get; set; }
    ///
    ///     [Column("Name", Length = 100, IsNullable = false)]
    ///     public string Name { get; set; } = string.Empty;
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TableAttribute(string name) : Attribute
    {
        /// <summary>
        /// Der Tabellenname in der Datenbank.
        /// </summary>
        public string Name { get; } = name;
    }

    /// <summary>
    /// Markiert eine Eigenschaft als Datenbankspalte.
    /// </summary>
    /// <remarks>
    /// Dieses Attribut definiert die Zuordnung eines Properties zu einer Spalte in der Datenbanktabelle.
    /// Über die optionalen Eigenschaften können Nullable-Verhalten und Länge (bei Strings) festgelegt werden.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// [Column("Email", IsNullable = false, Length = 255)]
    /// public string Email { get; set; } = string.Empty;
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public sealed class ColumnAttribute(string name) : Attribute
    {
        /// <summary>
        /// Der Spaltenname in der Tabelle.
        /// </summary>
        public string Name { get; } = name;

        /// <summary>
        /// Gibt an, ob <c>NULL</c>-Werte erlaubt sind.
        /// </summary>
        /// <value>Standardwert: <c>true</c>.</value>
        public bool IsNullable { get; set; } = true;

        /// <summary>
        /// Gibt die maximale Länge für Textspalten an (nur relevant für <see cref="string"/>).
        /// </summary>
        /// <value>
        /// Standardwert: <c>0</c> (keine Längenbegrenzung).
        /// </value>
        public int Length { get; set; } = 0;
    }

    /// <summary>
    /// Markiert eine Eigenschaft als Primärschlüssel der Tabelle.
    /// </summary>
    /// <remarks>
    /// Nur ein Property pro Entität sollte mit <see cref="PrimaryKeyAttribute"/> versehen sein.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// [PrimaryKey]
    /// [Column("Id")]
    /// public int Id { get; set; }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public sealed class PrimaryKeyAttribute : Attribute { }

    /// <summary>
    /// Markiert eine Eigenschaft als Auto-Increment-Spalte.
    /// </summary>
    /// <remarks>
    /// Dieses Attribut ist in der Regel in Kombination mit <see cref="PrimaryKeyAttribute"/> zu verwenden.
    /// Die Spalte wird in SQLite automatisch hochgezählt, wenn neue Datensätze eingefügt werden.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// [PrimaryKey]
    /// [AutoIncrement]
    /// [Column("Id")]
    /// public int Id { get; set; }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public sealed class AutoIncrementAttribute : Attribute { }

    /// <summary>
    /// Markiert eine Eigenschaft als Fremdschlüssel.
    /// </summary>
    /// <remarks>
    /// Dieses Attribut definiert eine referenzielle Beziehung zwischen zwei Tabellen.
    /// Die Fremdschlüsseldefinition wird beim Erstellen der Tabelle berücksichtigt.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// [ForeignKey(typeof(User), "Id", OnDelete = OnDeleteAction.Cascade)]
    /// [Column("UserId")]
    /// public int UserId { get; set; }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public sealed class ForeignKeyAttribute : Attribute
    {
        /// <summary>
        /// Der Typ der referenzierten Entität (Principal-Tabelle).
        /// </summary>
        public Type PrincipalEntity { get; }

        /// <summary>
        /// Der Name der referenzierten Spalte in der Principal-Tabelle.
        /// </summary>
        public string PrincipalColumn { get; }

        /// <summary>
        /// Gibt an, welche Aktion bei Löschung der Principal-Zeile ausgeführt werden soll.
        /// </summary>
        /// <value>
        /// Standardwert: <see cref="OnDeleteAction.NoAction"/>.
        /// </value>
        public OnDeleteAction OnDelete { get; set; } = OnDeleteAction.NoAction;

        /// <summary>
        /// Initialisiert ein neues <see cref="ForeignKeyAttribute"/>.
        /// </summary>
        /// <param name="principalEntity">Der Typ der referenzierten Entität.</param>
        /// <param name="principalColumn">Der Name der referenzierten Spalte.</param>
        public ForeignKeyAttribute(Type principalEntity, string principalColumn)
            => (PrincipalEntity, PrincipalColumn) = (principalEntity, principalColumn);
    }

    /// <summary>
    /// Schließt eine Eigenschaft vom Mapping aus.
    /// </summary>
    /// <remarks>
    /// Dieses Attribut kann verwendet werden, um Properties zu kennzeichnen, die nicht
    /// in die Datenbanktabelle übernommen werden sollen (z. B. Navigationseigenschaften oder berechnete Werte).
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// [Ignore]
    /// public ICollection&lt;Post&gt; Posts { get; set; } = new List&lt;Post&gt;();
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public sealed class IgnoreAttribute : Attribute { }
}
