namespace BackendGenerator.Core.Models;

public enum DeleteBehavior
{
    None,       // No cascade (default)
    Cascade,
    Restrict,
    SetNull,
    NoAction
}

public class PropertyDefinition
{
    public string Name { get; set; }
    public string Type { get; set; }
    public bool PrimaryKey { get; set; } = false;
    public bool Required { get; set; } = false;
    public string ForeignKey { get; set; }  // Format: "OtherEntity.Property"

    // New property for delete behavior on foreign key
    public DeleteBehavior? OnDelete { get; set; }
}
