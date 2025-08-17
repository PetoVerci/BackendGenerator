using BackendGenerator.Core.Interfaces;
using BackendGenerator.Core.Models.hyml;

namespace BackendGenerator.Infrastructure.Validators;

public class HymlModelValidator : IModelValidator
{
    public List<string> Validate(HymlModel m)
    {
        var errors = new List<string>();

        ValidateMetadata(m, errors);
        ValidateEntities(m, errors);
        ValidateRelations(m, errors);

        return errors;
    }

    private void ValidateMetadata(HymlModel m, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(m.Version))
            errors.Add("Model: version is required.");

        if (string.IsNullOrWhiteSpace(m.Name))
            errors.Add("Model: name is required.");
    }

    private void ValidateEntities(HymlModel m, List<string> errors)
    {
        if (m.Entities == null || m.Entities.Count == 0)
        {
            errors.Add("Model: at least one entity is required.");
            return;
        }

        foreach (var (entityName, entity) in m.Entities)
        {
            ValidatePrimaryKeys(entityName, entity, errors);
        }
    }

    private void ValidatePrimaryKeys(string entityName, Entity e, List<string> errors)
    {
        var pkCount = e.Fields.Values.Count(f => f.Pk);

        if (pkCount == 0)
            errors.Add($"Entity {entityName}: must have a primary key.");
    }

    private void ValidateRelations(HymlModel m, List<string> errors)
    {
        if (m.Entities == null) return;

        foreach (var (entityName, entity) in m.Entities)
        {
            if (entity.Relations == null) continue;

            foreach (var relation in entity.Relations)
            {
                var toEntityName = relation.To.Split('.')[0];
                if (!m.Entities.ContainsKey(toEntityName))
                {
                    errors.Add($"Relation {entityName}.{relation.From} -> {relation.To}: references missing entity {toEntityName}.");
                }
                else
                {
                    // Optional: Check if the target column exists
                    var toCol = relation.To.Contains(".") ? relation.To.Split('.')[1] : "id";
                    if (!m.Entities[toEntityName].Fields.ContainsKey(toCol))
                    {
                        errors.Add($"Relation {entityName}.{relation.From} -> {relation.To}: target column {toCol} not found in {toEntityName}.");
                    }
                }
            }
        }
    }
}
