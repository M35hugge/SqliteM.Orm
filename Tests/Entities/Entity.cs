using SQLiteM.Abstractions;
using System;

namespace Tests.Entities
{
    [Table("Person")]
    public sealed class Person
    {
        [PrimaryKey, AutoIncrement]
        [Column]
        public int Id { get; set; }

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
        [Column]
        public int Id { get; set; }

        [Column(IsNullable = false)]
        [ForeignKey(typeof(Person), nameof(Person.Id), OnDelete = OnDeleteAction.Cascade)]
        public int PersonId { get; set; }

        [Column("Total", IsNullable = false)]
        public decimal Total { get; set; }

        [Column("Note", IsNullable = true, Length = 200)]
        public string? Note { get; set; }

    }
    [Table("people_q")]
    public sealed class PersonQ
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        [Column("first_name", IsNullable = false, Length = 100)]
        public string FirstName { get; set; } = default!;

        [Column("last_name", IsNullable = false, Length = 100)]
        public string LastName { get; set; } = default!;

        [Column("age", IsNullable = false)]
        public int Age { get; set; }

        [Column("created_at", IsNullable = false)]
        public DateTime CreatedAt { get; set; }

        [Column("email", IsNullable = true, Length = 255)]
        public string? Email { get; set; }
    }
}
