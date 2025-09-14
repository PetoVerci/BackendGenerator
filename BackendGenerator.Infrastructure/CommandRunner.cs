using BackendGenerator.Core.Interfaces;
using BackendGenerator.Core.Models;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BackendGenerator.Infrastructure;

public class CommandRunner : ICommandRunner
{
    private readonly ILogger _logger;

    public CommandRunner(ILogger logger)
    {
        _logger = logger;
    }

    private static CommandResult RunCommand(string command, string args, string? workingDirectory = null)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new CommandResult
        {
            ExitCode = process.ExitCode,
            Output = output,
            Error = error
        };
    }

    public Result<CommandResult> RunAndCheck(string command, string args, string? workingDir = null)
    {
        var result = RunCommand(command, args, workingDir);
        if (result.ExitCode != 0)
        {
            if (string.IsNullOrWhiteSpace(result.Error))
            {
                _logger.LogError("Command '{command}' with args '{args}' failed. Output :{newline}'{output}'", command, args, Environment.NewLine, result.Output);
            }
            else
            {
                _logger.LogError("Command '{command}' with args '{args}' failed. Errors :{newline}'{error}'", command, args, Environment.NewLine, result.Error);
            }
            return Result.Fail(result.Error);
        }
        return Result.Ok(result);
    }
}
