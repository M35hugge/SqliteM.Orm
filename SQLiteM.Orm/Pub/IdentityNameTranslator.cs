using SQLiteM.Abstractions;

namespace SQLiteM.Orm.Pub
{
    /// <summary>
    /// Standard-Übersetzer, der die Namen unverändert durchreicht.
    /// </summary>
    /// <remarks>
    /// Dies ist die Default-Strategie: Tabellen- und Spaltennamen entsprechen
    /// den CLR-Namen, sofern kein expliziter Name über Attribute gesetzt ist.
    /// </remarks>
    public class IdentityNameTranslator : INameTranslator
    {
        /// <inheritdoc />
        public string Column(string clrPropertyName) => clrPropertyName;

        /// <inheritdoc />
        public string Table(string clrTypeName) =>clrTypeName;

        /// <inheritdoc />
        public string Property(string clrColumnName) => clrColumnName;
    }
}
