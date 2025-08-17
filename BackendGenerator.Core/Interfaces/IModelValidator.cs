using BackendGenerator.Core.Models.hyml;
using FluentResults;

namespace BackendGenerator.Core.Interfaces;

public interface IModelValidator
{
    List<string> Validate(HymlModel m);
}
