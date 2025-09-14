using BackendGenerator.Core.Interfaces;
using BackendGenerator.Infrastructure.FileWriters;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;

namespace BackendGenerator.Infrastructure;

public class WebApiProjectGenerator
{
    private readonly AppSettingsDbConnectionWriter _connectionWriter;
    private readonly RepositoryGenerator _repositoryGenerator;
    private readonly ProgramGenerator _programGenerator;
    private readonly ILogger<WebApiProjectGenerator> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly ICommandRunner _commandRunner;

    private readonly string[] necessaryPackages =
    [
        "Npgsql.EntityFrameworkCore.PostgreSQL",
        "Microsoft.EntityFrameworkCore.Design",
        "Microsoft.EntityFrameworkCore.Tools",
        "Swashbuckle.AspNetCore",
        "Microsoft.VisualStudio.Web.CodeGeneration.Design",
        "Microsoft.EntityFrameworkCore.SqlServer"
    ];

    public WebApiProjectGenerator(ICommandRunnerFactory commandRunnerFactory, AppSettingsDbConnectionWriter connectionWriter,
        ILogger<WebApiProjectGenerator> logger, IFileSystem fileSystem, RepositoryGenerator repositoryGenerator,
        ProgramGenerator programGenerator)
    {
        _commandRunner = commandRunnerFactory.Create<WebApiProjectGenerator>();
        _connectionWriter = connectionWriter;
        _logger = logger;
        _fileSystem = fileSystem;
        _repositoryGenerator = repositoryGenerator;
        _programGenerator = programGenerator;
    }

    public Result Execute(string projectGenerationPath, string applicationName, string dbConnectionString,
        List<string> generateRepositoriesFor, List<string> generateCrudControllersFor)
    {
        string projectName = $"{applicationName}Api";
        string projectPath = _fileSystem.Path.Combine(projectGenerationPath, projectName);
        string dbContextName = $"{applicationName}DbContext";

        try
        {
            // Prepare project folder
            if (_fileSystem.Directory.Exists(projectGenerationPath))
            {
                _logger.LogWarning("Specified directory '{projectPath}' exists. Deleting old content...", projectGenerationPath);
                _fileSystem.Directory.Delete(projectGenerationPath, true);
            }
            _fileSystem.Directory.CreateDirectory(projectGenerationPath);

            _logger.LogInformation("Creating new web API project");

            // Create web API project
            var apiResult = _commandRunner.RunAndCheck("dotnet", $"new webapi -o \"{projectPath}\" -n {projectName}");
            if (apiResult.IsFailed)
            {
                return apiResult.ToResult();
            }

            _logger.LogInformation("Adding necessary packages");
            // Install necessary packages
            foreach (var pkg in necessaryPackages)
            {
                var pkgResult = _commandRunner.RunAndCheck("dotnet", $"add \"{projectPath}\" package {pkg}");
                if (pkgResult.IsFailed)
                {
                    return pkgResult.ToResult();
                }
            }

            _logger.LogInformation("Installing ASP.NET code generator");
            // Install aspnet code generator
            var toolInstall = _commandRunner.RunAndCheck("dotnet", "tool install --global dotnet-aspnet-codegenerator");
            if (toolInstall.IsFailed)
            {
                return toolInstall.ToResult();
            }

            _logger.LogInformation("Scaffolding database into DbContext and Entities");
            // Scaffold DbContext and entities
            var scaffoldResult = _commandRunner.RunAndCheck(
                "dotnet",
                $"ef dbcontext scaffold \"{dbConnectionString}\" Npgsql.EntityFrameworkCore.PostgreSQL --output-dir Models --context-dir Data -c {dbContextName} --no-onconfiguring --force",
                projectPath
            );
            if (scaffoldResult.IsFailed)
            {
                return scaffoldResult.ToResult();
            }

            // Write connection string to appsettings <- NOT SURE IF EVEN NECESSARY, PROBABLY WILL BE SOLVED BY SOME .env FILE
            string appsettingsPath = _fileSystem.Path.Combine(projectPath, "appsettings.Development.json");
            var connResult = _connectionWriter.WriteToFile(appsettingsPath, dbConnectionString);

            if (connResult.IsFailed)
            {
                return connResult;
            }

            bool generateRepositories = generateRepositoriesFor.Any();
            bool generateControllers = generateCrudControllersFor.Any();
            // Decide which program template to choose
            string correctProgramTemplateName = generateRepositories ? Constants.ProgramTemplateWithRepositories : Constants.ProgramTemplateNoRepositories;

            // Load Program.cs template
            string programTemplate = _fileSystem.File.ReadAllText(correctProgramTemplateName);


            // Generate repositories if specified
            if (generateRepositories)
            {
                _logger.LogInformation("Generating CRUD Repositories");
                GenerateRepositories(generateRepositoriesFor, projectName, projectPath, dbContextName);
            }

            _programGenerator.EmitProgramFile(projectPath, programTemplate, projectName, dbContextName);

            if (generateControllers)
            {
                _logger.LogInformation("Generating CRUD Controllers");
                GenerateControllers(generateCrudControllersFor, dbContextName , projectPath);
            }
            _logger.LogInformation("Web API project created successfully.");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during project generation.");
            return Result.Fail(ex.Message);
        }
    }

    private void GenerateRepositories(List<string> generateRepositoriesFor, string projectName, string projectPath, string dbContextName)
    {
        // Load repository templates
        string repoTemplate = _fileSystem.File.ReadAllText(Constants.RepositoryTemplate);
        string repoInterfaceTemplate = _fileSystem.File.ReadAllText(Constants.IRepositoryTemplate);

        string repositoryFolderPath = _fileSystem.Path.Combine(projectPath, "Repositories");
        _fileSystem.Directory.CreateDirectory(repositoryFolderPath);

        _repositoryGenerator.EmitRepositoryInterface(repositoryFolderPath, repoInterfaceTemplate, projectName);
        _repositoryGenerator.EmitRepositories(generateRepositoriesFor, repositoryFolderPath, repoTemplate, projectName, dbContextName);
    }

    private Result<string> GenerateControllers(List<string> generateCrudControllersFor, string dbContextName, string projectPath)
    {
        const string ControllerCommandTemplate =
"aspnet-codegenerator controller -name {CONTROLLER} -async -api -m {MODEL} -dc {DBCONTEXT} -outDir Controllers";

        foreach (var entity in generateCrudControllersFor)
        {
            var controllerName = $"{entity}Controller";

            var args = ControllerCommandTemplate
                .Replace("{CONTROLLER}", controllerName)
                .Replace("{MODEL}", entity)
                .Replace("{DBCONTEXT}", dbContextName);

            var controllerResult = _commandRunner.RunAndCheck("dotnet", args, projectPath);
            if (controllerResult.IsFailed)
            {
                return controllerResult.ToResult();
            }
        }
        return Result.Ok();
    }
}
