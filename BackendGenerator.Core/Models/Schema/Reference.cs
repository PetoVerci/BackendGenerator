namespace BackendGenerator.Core.Models.Schema;

public class Reference
{
    public required string FromColumn;
    public required string ToTable;
    public required string ToColumn;
    public string? OnDelete;
    public string? OnUpdate;
}