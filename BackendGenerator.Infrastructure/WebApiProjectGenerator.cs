using BackendGenerator.Infrastructure.FileWriters;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;

namespace BackendGenerator.Infrastructure;

public class WebApiProjectGenerator
{
    private readonly CommandRunner _commandRunner;
    private readonly AppSettingsDbConnectionWriter _connectionWriter;
    private readonly RepositoryGenerator _repositoryGenerator;
    private readonly ProgramGenerator _programGenerator;
    private readonly ILogger<WebApiProjectGenerator> _logger;
    private readonly IFileSystem _fileSystem;

    private readonly string[] necessaryPackages = new[]
    {
        "Npgsql.EntityFrameworkCore.PostgreSQL",
        "Microsoft.EntityFrameworkCore.Design",
        "Microsoft.EntityFrameworkCore.Tools",
        "Swashbuckle.AspNetCore",
        "Microsoft.VisualStudio.Web.CodeGeneration.Design",
        "Microsoft.EntityFrameworkCore.SqlServer"
    };

    public WebApiProjectGenerator(CommandRunner commandRunner, AppSettingsDbConnectionWriter connectionWriter,
        ILogger<WebApiProjectGenerator> logger, IFileSystem fileSystem, RepositoryGenerator repositoryGenerator,
        ProgramGenerator programGenerator)
    {
        _commandRunner = commandRunner;
        _connectionWriter = connectionWriter;
        _logger = logger;
        _fileSystem = fileSystem;
        _repositoryGenerator = repositoryGenerator;
        _programGenerator = programGenerator;
    }

    public Result Execute(string projectGenerationPath, string applicationName, string dbConnectionString)
    {
        string projectName = $"{applicationName}Api";
        string projectPath = _fileSystem.Path.Combine(projectGenerationPath, projectName);
        string dbContextName = $"{applicationName}DbContext";

        // Helper for running commands
        Result RunAndCheck(string command, string args, string? workingDir = null)
        {
            var result = _commandRunner.RunCommand(command, args, workingDir);
            if (result.ExitCode != 0)
            {
                _logger.LogError("Command '{command} {args}' failed: {error}", command, args, result.Error);
                return Result.Fail(result.Error);
            }
            return Result.Ok();
        }

        try
        {
            // Prepare project folder
            if (_fileSystem.Directory.Exists(projectGenerationPath))
            {
                _logger.LogWarning("Specified directory '{projectPath}' exists. Deleting old content...", projectGenerationPath);
                _fileSystem.Directory.Delete(projectGenerationPath, true);
            }
            _fileSystem.Directory.CreateDirectory(projectGenerationPath);

            // Create web API project
            var apiResult = RunAndCheck("dotnet", $"new webapi -o \"{projectPath}\" -n {projectName}");
            if (apiResult.IsFailed) return apiResult;

            // Install necessary packages
            foreach (var pkg in necessaryPackages)
            {
                var pkgResult = RunAndCheck("dotnet", $"add \"{projectPath}\" package {pkg}");
                if (pkgResult.IsFailed) return pkgResult;
            }

            // install aspnet code generator
            var toolInstall = RunAndCheck("dotnet", "tool install --global dotnet-aspnet-codegenerator");

            // Scaffold DbContext and entities
            var scaffoldResult = RunAndCheck(
                "dotnet",
                $"ef dbcontext scaffold \"{dbConnectionString}\" Npgsql.EntityFrameworkCore.PostgreSQL --output-dir Models --context-dir Data -c {dbContextName} --no-onconfiguring --force",
                projectPath
            );
            if (scaffoldResult.IsFailed) return scaffoldResult;

            // Write connection string to appsettings
            string appsettingsPath = _fileSystem.Path.Combine(projectPath, "appsettings.Development.json");
            var connResult = _connectionWriter.WriteToFile(appsettingsPath, dbConnectionString);
            if (connResult.IsFailed)
            {
                _logger.LogError("Failed to write connection string to appsettings file");
                return connResult;
            }

            // Load repository templates
            string repoTemplate = _fileSystem.File.ReadAllText("Resources/RepositoryTemplate");
            string repoInterfaceTemplate = _fileSystem.File.ReadAllText("Resources/IRepositoryTemplate");

            // Load Program.cs template
            string programTemplate = _fileSystem.File.ReadAllText("Resources/ProgramTemplate");

            // Retrieve entity names
            var entityFiles = _fileSystem.Directory.GetFiles(_fileSystem.Path.Combine(projectPath, "Models"), "*.cs");
            var entityNames = entityFiles.Select(_fileSystem.Path.GetFileNameWithoutExtension);

            // Generate repositories
            string repositoryFolderPath = _fileSystem.Path.Combine(projectPath, "Repositories");
            _fileSystem.Directory.CreateDirectory(repositoryFolderPath);

            _repositoryGenerator.EmitRepositoryInterface(repositoryFolderPath, repoInterfaceTemplate, projectName);
            _repositoryGenerator.EmitRepositories(entityNames, repositoryFolderPath, repoTemplate, projectName, dbContextName);

            _programGenerator.EmitProgramFile(projectPath, programTemplate, projectName, dbContextName);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during project generation.");
            return Result.Fail(ex.Message);
        }
    }
}
