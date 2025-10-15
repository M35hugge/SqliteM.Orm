using System.Runtime.CompilerServices;

namespace SQLiteM.Abstractions;

/// <summary>
/// Grundlegende Abstraktionen und Metadaten für das SQLiteM-ORM.
/// </summary>
/// <remarks>
/// Der Namespace enthält:
/// <list type="bullet">
/// <item><description>Attribute für Mapping und Schema: <see cref="TableAttribute"/>, <see cref="ColumnAttribute"/>, <see cref="PrimaryKeyAttribute"/>, <see cref="AutoIncrementAttribute"/>, <see cref="ForeignKeyAttribute"/>, <see cref="IgnoreAttribute"/>.</description></item>
/// <item><description>Schnittstellen für Verbindung, Dialekt, Mapping, SQL-Generierung und Repositories: <see cref="IConnectionFactory"/>, <see cref="ISqlDialect"/>, <see cref="IEntityMapper"/>, <see cref="ISqlBuilder"/>, <see cref="IRepository{T}"/> u. a.</description></item>
/// <item><description>Hilfsdatentypen für das Mapping: <see cref="PropertyMap"/> und <see cref="ForeignKeyMap"/> sowie <see cref="OnDeleteAction"/>.</description></item>
/// </list>
/// Typischerweise hängt Anwendungs-Code nur von diesen Abstraktionen ab; die konkreten Implementierungen liegen in <c>SQLiteM.Orm</c>.
/// </remarks>
[CompilerGenerated]
public static class NamespaceDoc { }
