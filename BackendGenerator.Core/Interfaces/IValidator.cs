using BackendGenerator.Core.Models;
using FluentResults;

namespace BackendGenerator.Core.Interfaces;

public interface IValidator
{
    Result<List<string>> Validate(Model m);
}
