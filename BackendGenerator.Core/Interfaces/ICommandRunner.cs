using BackendGenerator.Core.Models;

namespace BackendGenerator.Core.Interfaces;

public interface ICommandRunner
{
    CommandResult RunCommand(string command, string args, string? workingDirectory = null);
}
