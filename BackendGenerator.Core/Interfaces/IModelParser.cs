using BackendGenerator.Core.Models;
using FluentResults;

namespace BackendGenerator.Core.Interfaces;

public interface IModelParser
{
    Result<ParsedModelDefinition> Parse(string filePath);
}
