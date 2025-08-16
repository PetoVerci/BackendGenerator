using BackendGenerator.Core.Interfaces;
using BackendGenerator.Core.Models;
using FluentResults;
using System.Text;
using System.Collections.Generic;

namespace BackendGenerator.Infrastructure.Emmiters;

public class DbmlEmiter : IEmiter
{
    public DbmlEmiter() { }

    public Result<string> Emit(Model m)
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

        // 3. Emit DBML
        var sb = new StringBuilder();
        foreach (var table in tableRegistry.Values)
        {
            sb.AppendLine($"Table {table.Name} {{");

            foreach (var (fname, f) in table.Fields)
            {
                var attrs = new List<string>();
                if (f.Pk) attrs.Add("pk");
                if (f.Unique) attrs.Add("unique");
                if (f.Required || f.Pk) attrs.Add("not null");

                sb.AppendLine($"  {fname} {MapType(f.Type)} [{string.Join(", ", attrs)}]");
            }

            // Emit unique composite indexes
            foreach (var idx in table.UniqueIndexes)
            {
                sb.AppendLine($"  indexes {{ ({string.Join(", ", idx)}) [unique] }}");
            }

            sb.AppendLine("}\n");

            foreach (var r in table.References)
            {
                var actions = new List<string>();
                if (!string.IsNullOrWhiteSpace(r.OnDelete)) actions.Add($"delete: {r.OnDelete}");
                if (!string.IsNullOrWhiteSpace(r.OnUpdate)) actions.Add($"update: {r.OnUpdate}");

                string actionStr = actions.Count > 0 ? $" [{string.Join(", ", actions)}]" : "";
                sb.AppendLine($"Ref: {table.Name}.{r.FromColumn} > {r.ToTable}.{r.ToColumn}{actionStr}");
            }

            if (table.References.Count > 0)
                sb.AppendLine();
        }

        return sb.ToString();
    }

    private string MapType(string t) => t.StartsWith("decimal(") ? t : t switch
    {
        "string" => "varchar",
        "text" => "text",
        "int" => "int",
        "long" => "bigint",
        "float" => "float",
        "double" => "double",
        "decimal" => "decimal(18,2)",
        "bool" => "boolean",
        "uuid" => "uuid",
        "date" => "date",
        "time" => "time",
        "datetime" => "timestamptz",
        _ => t
    };

    private (string table, string col) Split(string path)
    {
        var parts = path.Split('.', 2);
        return (parts[0], parts.Length > 1 ? parts[1] : "id");
    }

    private string CreateFkString(string id) => id.Replace(".", "");

    private class TableDefinition
    {
        public string Name;
        public Dictionary<string, Field> Fields = new();
        public List<Reference> References = new();
        public List<List<string>> UniqueIndexes = new();  // <-- unique composite indexes
        public TableDefinition(string name) => Name = name;
    }

    private class Reference
    {
        public string FromColumn;
        public string ToTable;
        public string ToColumn;
        public string? OnDelete;
        public string? OnUpdate;
    }
}
