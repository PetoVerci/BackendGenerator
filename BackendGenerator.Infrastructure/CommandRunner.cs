using BackendGenerator.Core.Interfaces;
using BackendGenerator.Core.Models;
using System.Diagnostics;

public class CommandRunner : ICommandRunner
{
    public CommandResult RunCommand(string command, string args)
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
                CreateNoWindow = true
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
}
