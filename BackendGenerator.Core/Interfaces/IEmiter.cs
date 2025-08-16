using BackendGenerator.Core.Models;
using FluentResults;

namespace BackendGenerator.Core.Interfaces;

public interface IEmiter
{
    Result<string> Emit(Model m);
}
