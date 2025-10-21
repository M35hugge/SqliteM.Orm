using SQLiteM.Abstractions;

namespace Tests.Entities
{
    [Table("Person")]
    public sealed class Person
    {
        [PrimaryKey, AutoIncrement]
        [Column("Id")]
        public long Id { get; set; }

        [Column("FirstName", IsNullable = false, Length = 100)]
        public string FirstName { get; set; } = default!;

        [Column("LastName", IsNullable = false, Length = 100)]
        public string LastName { get; set; } = default!;

        [Column("Email", IsNullable = true, Length = 255)]
        public string? Email { get; set; }
    }

    [Table("Order")]
    public sealed class Order
    {
        [PrimaryKey, AutoIncrement]
        [Column("Id")]
        public long Id { get; set; }

        [Column("PersonId", IsNullable = false)]
        [ForeignKey(typeof(Person), nameof(Person.Id), OnDelete = OnDeleteAction.Cascade)]
        public long PersonId { get; set; }

        [Column("Total", IsNullable = false)]
        public decimal Total { get; set; }

        [Column("Note", IsNullable = true, Length = 200)]
        public string? Note { get; set; }
    }
}
