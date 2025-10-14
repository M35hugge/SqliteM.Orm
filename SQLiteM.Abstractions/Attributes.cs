using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteM.Abstractions;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TableAttribute(string name) : Attribute { public string Name { get; } = name; }

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class ColumnAttribute : Attribute
{

    public string Name { get; } 
    public bool IsNullable { get; set; } = true;
    public int Length { get; set; } = 0;

    public ColumnAttribute(String name) => Name = name;

}

[AttributeUsage(AttributeTargets.Property, Inherited =false)]
public sealed class PrimaryKeyAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property, Inherited =false)]
public sealed class AutoIncrementAttribute : Attribute { }