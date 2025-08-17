using BackendGenerator.Core.Models.hyml;

namespace BackendGenerator.Core.Models.Schema;

public class TableDefinition
{
    public string Name;
    public Dictionary<string, Field> Fields = new();
    public List<Reference> References = new();
    public List<List<string>> UniqueIndexes = new();  // <-- unique composite indexes
    public TableDefinition(string name) => Name = name;
}