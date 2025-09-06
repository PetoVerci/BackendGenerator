using BackendGenerator.Core.Interfaces;
using BackendGenerator.Core.Models.Schema;
using System.Text;

namespace BackendGenerator.Infrastructure.Emmiters
{
    public class SqlEmitter
    {
        public static string Emit(TableRegistry registry)
        {
            var sb = new StringBuilder();

            // Emit tables first
            foreach (var table in registry.TableDefinitions.Values)
            {
                sb.AppendLine(EmitTable(table));
            }

            // Emit foreign keys
            foreach (var table in registry.TableDefinitions.Values)
            {
                foreach (var fk in table.References)
                {
                    sb.AppendLine(EmitForeignKey(table.Name, fk));
                }
            }

            return sb.ToString();
        }

        private static string EmitTable(TableDefinition table)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE \"{table.Name}\" ("); // Quote table names

            var cols = new List<string>();
            foreach (var kvp in table.Fields)
            {
                var fieldName = kvp.Key;
                var field = kvp.Value;

                var parts = new List<string>
                {
                    $"\"{fieldName}\"",  // Quote column names
                    MapType(field.Type)
                };

                if (field.Pk)
                {
                    parts.Add("PRIMARY KEY");
                    if (field.PkGenerated && field.Type == "uuid")
                        parts.Add("DEFAULT gen_random_uuid()");
                }

                if (field.Required && !field.Pk)
                    parts.Add("NOT NULL");

                if (field.Unique && !field.Pk)
                    parts.Add("UNIQUE");

                cols.Add("  " + string.Join(" ", parts));
            }

            sb.AppendLine(string.Join(",\n", cols));

            // Composite unique indexes (inside table parentheses)
            foreach (var idx in table.UniqueIndexes)
            {
                sb.AppendLine($",  UNIQUE({string.Join(", ", idx.Select(c => $"\"{c}\""))})");
            }

            sb.AppendLine(");");
            return sb.ToString();
        }

        private static string EmitForeignKey(string tableName, Reference fk)
        {
            string constraint = $"fk_{tableName}_{fk.FromColumn}_{fk.ToTable}_{fk.ToColumn}".ToLower();
            return $@"
ALTER TABLE ""{tableName}""
ADD CONSTRAINT {constraint}
FOREIGN KEY (""{fk.FromColumn}"")
REFERENCES ""{fk.ToTable}""(""{fk.ToColumn}"")
ON DELETE {fk.OnDelete ?? "NO ACTION"}
ON UPDATE {fk.OnUpdate ?? "NO ACTION"};";
        }

        private static string MapType(string type) => type switch
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
            _ => type
        };
    }
}
