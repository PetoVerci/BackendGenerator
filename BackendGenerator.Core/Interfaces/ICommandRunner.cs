using BackendGenerator.Core.Models;
using FluentResults;

namespace BackendGenerator.Core.Interfaces;

public interface ICommandRunner
{
    Result<CommandResult> RunAndCheck(string command, string args, string? workingDir = null);
}
