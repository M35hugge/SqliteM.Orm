using SQLiteM.Abstractions;

namespace SQLiteM.Demo
{
    [Table("Person")]
    public sealed class Person
    {
        [PrimaryKey]
        [ColumnAttribute("Id")]
        public long Id { get; set; }

        [ColumnAttribute("FirstName", IsNullable = false, Length = 100)]
        public string FirstName { get; set; } = default!;

        [ColumnAttribute("LastName", IsNullable = false, Length = 100)]
        public string LastName { get; set; } = default!;

        [ColumnAttribute("Email", IsNullable = true, Length = 255)]
        public string? Email { get; set; }
    }
}
