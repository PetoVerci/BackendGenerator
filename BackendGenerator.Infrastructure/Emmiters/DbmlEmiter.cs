using BackendGenerator.Core.Interfaces;
using FluentResults;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using BackendGenerator.Core.Models.hyml;
using BackendGenerator.Core.Models.Schema;

namespace BackendGenerator.Infrastructure.Emmiters;

public class DbmlEmiter : IEmiter
{
    public string Emit(TableRegistry tableRegistry)
    {

        // 3. Emit DBML
        var sb = new StringBuilder();

        // Emit all tables first (sorted alphabetically for consistency)
        foreach (var table in tableRegistry.TableDefinitions.Values.OrderBy(t => t.Name))
        {
            sb.AppendLine($"Table {table.Name} {{");

            foreach (var (fname, f) in table.Fields.OrderBy(x => x.Key))
            {
                List<string> attrs = new();
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
        }

        // Emit references at the very end
        foreach (var table in tableRegistry.TableDefinitions.Values.OrderBy(t => t.Name))
        {
            foreach (var r in table.References)
            {
                List<string> actions = new();
                if (!string.IsNullOrWhiteSpace(r.OnDelete)) actions.Add($"delete: {r.OnDelete}");
                if (!string.IsNullOrWhiteSpace(r.OnUpdate)) actions.Add($"update: {r.OnUpdate}");

                string actionStr = actions.Count > 0 ? $" [{string.Join(", ", actions)}]" : "";
                sb.AppendLine($"Ref: {table.Name}.{r.FromColumn} > {r.ToTable}.{r.ToColumn}{actionStr}");
            }
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
}
