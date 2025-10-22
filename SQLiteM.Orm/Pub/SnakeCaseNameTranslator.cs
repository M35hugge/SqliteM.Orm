using SQLiteM.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteM.Orm.Pub
{
    /// <summary>
    /// Übersetzer, der CLR-Namen in <c>snake_case</c> konvertiert und (optional) umgekehrt.
    /// </summary>
    /// <remarks>
    /// Beispiele:
    /// - CLR: <c>PersonOrder</c> → <c>person_order</c>
    /// - CLR: <c>PersonId</c>    → <c>person_id</c>
    /// - DB : <c>first_name</c>  → <c>FirstName</c> (Property-Rückweg; nur wenn benötigt)
    /// </remarks>
    public class SnakeCaseNameTranslator : INameTranslator
    {
        /// <inheritdoc />
        public string Column(string clrPropertyName) => ToSnake(clrPropertyName);

        /// <inheritdoc />
        public string Table(string clrTypeName) => ToSnake(clrTypeName);

        /// <inheritdoc />
        public string Property(string fieldName) => SnakeToPascal(fieldName);

        private static string ToSnake(string clrName)
        {
            if(string.IsNullOrEmpty(clrName)) return clrName;
            var sb = new StringBuilder(clrName.Length+8);
            for (int i = 0; i<clrName.Length; i++)
            {
                var c = clrName[i];
                if(char.IsUpper(c))
                {
                    if (i > 0 && (char.IsLower(clrName[i-1])|| (i+1<clrName.Length && char.IsLower(clrName[i+1] ))))
                        sb.Append('_');
                    sb.Append(char.ToLowerInvariant(c));
                }
                else { sb.Append(c); }
            }
            return sb.ToString();   
        }
        private static string SnakeToPascal(string dbField)
        {
            if (string.IsNullOrEmpty(dbField)) return dbField;

            var sb = new StringBuilder(dbField.Length);
            bool upperNext = true;

            foreach (char c in dbField)
            {
                if (c == '_') { upperNext = true; continue; }
                sb.Append(upperNext ? char.ToUpperInvariant(c) : c);
                upperNext = false;
            }

            return sb.ToString();
        }

        // falls du lieber camelCase willst:
        private static string SnakeToCamel(string dbField)
        {
            var pascal = SnakeToPascal(dbField);
            if (string.IsNullOrEmpty(pascal)) return pascal;
            return char.ToLowerInvariant(pascal[0]) + pascal[1..];
        }
    }
}
