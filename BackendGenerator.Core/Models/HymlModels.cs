namespace BackendGenerator.Core.Models;

public record Model
{
    public string Version { get; init; } = "1.0"; // default version
    public string Name { get; init; } = string.Empty;
    public IDictionary<string, Entity> Entities { get; init; } = new Dictionary<string, Entity>();
    public Api? Api { get; init; }
}

public record Entity
{
    public IDictionary<string, Field> Fields { get; init; } = new Dictionary<string, Field>();
    public IList<Relation>? Relations { get; init; }
    public IList<string>? Traits { get; init; }
}

public record Field
{
    public string Type { get; init; } = string.Empty;
    public bool Required { get; init; } = false;
    public bool Pk { get; init; } = false;
    public bool Unique { get; init; } = false;
}

public record Relation
{
    public string Kind { get; init; } = string.Empty;            // "many-to-one" | "one-to-many" | "many-to-many"
    public string From { get; init; } = string.Empty;           // e.g., "Product.id"
    public string To { get; init; } = string.Empty;             // e.g., "Category.id"
    public string? Through { get; init; }                        // join table for n-n
    public string? OnDelete { get; init; }                       // e.g., "cascade" | "restrict" | "set_null"
    public string? OnUpdate { get; init; }                       // e.g., "cascade" | "restrict"
}

public record Api
{
    public IDictionary<string, Query>? Queries { get; init; }
    public IDictionary<string, Command>? Commands { get; init; }
}

public record Query
{
    public string Entity { get; init; } = string.Empty;
    public IList<string>? Filters { get; init; }
    public bool Pagination { get; init; } = false;
    public IList<string>? Sort { get; init; }
}

public record Command
{
    public string Entity { get; init; } = string.Empty;
    public IList<string> Input { get; init; } = new List<string>();
}
