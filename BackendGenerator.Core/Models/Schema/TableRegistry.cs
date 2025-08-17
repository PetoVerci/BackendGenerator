namespace BackendGenerator.Core.Models.Schema;

public class TableRegistry
{
    public required Dictionary<string, TableDefinition> TableDefinitions { get; init; }
}
