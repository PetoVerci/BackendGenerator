using BackendGenerator.Core.Interfaces;
using BackendGenerator.Core.Models;
using FluentResults;

namespace BackendGenerator.Infrastructure.Validators;

public class HymlModelValidator : IValidator
{
    public Result<List<string>> Validate(Model m)
    {
        var errs = new List<string>();
        if (string.IsNullOrWhiteSpace(m.Version)) errs.Add("version is required");
        if (string.IsNullOrWhiteSpace(m.Name)) errs.Add("name is required");
        if (m.Entities is null || m.Entities.Count == 0) errs.Add("at least one entity is required");

        // Check PKs
        foreach (var (en, e) in m.Entities)
        {
            var pkCount = e.Fields.Values.Count(f => f.Pk);
            if (pkCount == 0) errs.Add($"entity {en} must have a primary key");
        }

        // Basic relation target checks
        if (m.Entities != null)
            foreach (var (en, e) in m.Entities)
            {
                if (e.Relations == null) continue;
                foreach (var r in e.Relations)
                {
                    var toEntity = r.To.Split('.')[0];
                    if (!m.Entities.ContainsKey(toEntity))
                        errs.Add($"relation from {en} -> {r.To} references missing entity {toEntity}");
                }
            }

        return errs;
    }
}
