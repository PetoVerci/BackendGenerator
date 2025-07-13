namespace BackendGenerator.Core.Models;

public class ParsedModelDefinition
{
    public List<EntityDefinition> Entities { get; set; } = new();
    public List<QueryDefinition> Queries { get; set; } = new();
}
