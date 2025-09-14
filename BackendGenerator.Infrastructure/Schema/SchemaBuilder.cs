using BackendGenerator.Core.Interfaces;
using BackendGenerator.Core.Models.hyml;
using BackendGenerator.Core.Models.Schema;

namespace BackendGenerator.Infrastructure.Schema;

public class SchemaBuilder : ISchemaBuilder
{
    public TableRegistry Build(HymlModel m)
    {
        var tableRegistry = new Dictionary<string, TableDefinition>();

        // 1. Collect tables and fields
        foreach (var (name, e) in m.Entities)
        {
            if (!tableRegistry.ContainsKey(name))
                tableRegistry[name] = new TableDefinition(name);

            var table = tableRegistry[name];
            foreach (var (fname, f) in e.Fields)
                table.Fields[fname] = f;
        }

        // 2. Collect relations
        foreach (var (name, e) in m.Entities)
        {
            if (e.Relations == null) continue;

            foreach (var r in e.Relations)
            {
                var (fromTable, fromCol) = Split(r.From);
                var (toTable, toCol) = Split(r.To);

                if (r.Kind.Equals("many-to-many", StringComparison.OrdinalIgnoreCase))
                {
                    // Generate join table name automatically
                    string joinTableName = $"{fromTable}_{toTable}";

                    if (!tableRegistry.ContainsKey(joinTableName))
                        tableRegistry[joinTableName] = new TableDefinition(joinTableName);

                    var joinTable = tableRegistry[joinTableName];

                    // Add surrogate PK
                    if (!joinTable.Fields.ContainsKey("id"))
                        joinTable.Fields["id"] = new Field { Type = "uuid", Required = true, Pk = true };

                    // Add FK columns
                    string fk1 = CreateFkString(r.From);
                    string fk2 = CreateFkString(r.To);

                    if (!joinTable.Fields.ContainsKey(fk1))
                        joinTable.Fields[fk1] = new Field { Type = "uuid", Required = true };

                    if (!joinTable.Fields.ContainsKey(fk2))
                        joinTable.Fields[fk2] = new Field { Type = "uuid", Required = true };

                    // References
                    joinTable.References.Add(new Reference
                    {
                        FromColumn = fk1,
                        ToTable = fromTable,
                        ToColumn = "id",
                        OnDelete = r.OnDelete
                    });
                    joinTable.References.Add(new Reference
                    {
                        FromColumn = fk2,
                        ToTable = toTable,
                        ToColumn = "id",
                        OnDelete = r.OnDelete
                    });

                    // Unique composite index on the two FKs
                    joinTable.UniqueIndexes.Add(new List<string> { fk1, fk2 });
                }
                else
                {
                    // Normal relation
                    if (!tableRegistry.ContainsKey(fromTable))
                        tableRegistry[fromTable] = new TableDefinition(fromTable);

                    tableRegistry[fromTable].References.Add(new Reference
                    {
                        FromColumn = fromCol,
                        ToTable = toTable,
                        ToColumn = toCol,
                        OnDelete = r.OnDelete,
                        OnUpdate = r.OnUpdate
                    });
                }
            }
        }

        return new TableRegistry { TableDefinitions = tableRegistry };
    }

    private (string table, string col) Split(string path)
    {
        var parts = path.Split('.', 2);
        return (parts[0], parts.Length > 1 ? parts[1] : "id");
    }

    private string CreateFkString(string id) => id.Replace(".", "");
}
