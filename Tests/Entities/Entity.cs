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

    // Minimale Test-Entität
    [Table("people_tx")]
    public sealed class PersonTx
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        [Column("name", IsNullable = false, Length = 100)]
        public string Name { get; set; } = string.Empty;
    }

    // --- Test-Entities ---
    [Table("people_repo")]
    public sealed class PersonRepo
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        // explizit NOT NULL
        [Column("first_name", IsNullable = false, Length = 50)]
        public string FirstName { get; set; } = default!;

        // Name-Translator soll Camel->snake machen (CLR "LastName" -> DB "last_name")
        public string? LastName { get; set; }

        // Unique-Column (Spalten-UNIQUE)
        [Column("email", IsNullable = true, Length = 255, IsUniqueColumn = true)]
        public string? Email { get; set; }
    }

    // Entity ohne PK, um Fehlpfade zu testen
    [Table("no_pk_entities")]
    public sealed class NoPkEntity
    {
        [Column("value", IsNullable = true)]
        public string? Value { get; set; }
    }
}
