namespace BackendGenerator.Core.Models;

public class QueryDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // e.g., "top", "filter"

    public int? Count { get; set; } // for "top" queries
    public string? OrderBy { get; set; }
    public string? Filter { get; set; } // basic filter string for now
}
