using SQLiteM.Abstractions;

namespace SQLiteM.Demo
{
    [Table]
    public sealed class Person
    {
        [AutoIncrement]
        public int Id { get; set; }

        [Column( IsNullable = false, Length = 100)]
        public string FirstName { get; set; } = default!;

        [Column(IsNullable = false, Length = 100)]
        public string LastName { get; set; } = default!;

        [Column(IsNullable = true, Length = 255)]
        public string? Email { get; set; }

        [Ignore]
        public List<Order> Orders { get; } = []; // Navigation, wird nicht gemappt
    }
    [Table]
    public sealed class Order
    {
        [AutoIncrement]
        public int Id { get; set; }

        [Column(IsNullable = false)]
        [ForeignKey(typeof(Person), nameof(Person.Id), OnDelete = OnDeleteAction.Cascade)]
        public int PersonId { get; set; }

        [Column( IsNullable = false)]
        public decimal Total { get; set; }

        [Column(IsNullable = true, Length = 200)]
        public string? Note { get; set; }

        [Ignore]
        public Person? Person { get; set; } // Navigation
    }
}
