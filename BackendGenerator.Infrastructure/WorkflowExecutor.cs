using BackendGenerator.Core.Interfaces;
using BackendGenerator.Core.Models.hyml;
using BackendGenerator.Infrastructure.Parsers;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace BackendGenerator.Infrastructure;

public class WorkflowExecutor
{
    private readonly DatabaseContainerGenerationExecutor _persistanceLayerGenerator;
    private readonly WebApiProjectGenerator _webApiProjectGenerator;
    private readonly ILogger<WorkflowExecutor> _logger;
    private readonly HymlModelParser _hymlModelParser;
    private readonly ICommandRunner _commandRunner;
    public WorkflowExecutor(DatabaseContainerGenerationExecutor persistanceLayerGenerator, WebApiProjectGenerator webApiProjectGenerator,
        ILogger<WorkflowExecutor> logger, HymlModelParser hymlModelParser, ICommandRunnerFactory commandRunnerFactory)
    {
        _persistanceLayerGenerator = persistanceLayerGenerator;
        _webApiProjectGenerator = webApiProjectGenerator;
        _logger = logger;
        _hymlModelParser = hymlModelParser;
        _commandRunner = commandRunnerFactory.Create<WorkflowExecutor>();
    }

    public Result Execute(string pathToModel)
    {
        _logger.LogInformation("Workflow started");
        string? databaseDockerContainerName = null;

        try
        {
            // Parse the model file
            var parseResult = _hymlModelParser.Load(pathToModel);
            if (parseResult.IsFailed)
            {
                return Result.Fail(parseResult.Errors);
            }

            var model = parseResult.Value;

            // Generate the database
            var databaseGenerationResult = _persistanceLayerGenerator.Execute(model);
            databaseDockerContainerName = databaseGenerationResult.ContainerName;

            if (databaseGenerationResult.CommandResult.IsFailed)
            {
                return Result.Fail("Database generation failed");
            }

            // Prepare entity lists
            var entitiesWithCrudRepos = model.Entities
                .Where(e => e.Value.GenerateCrudRepositories)
                .Select(e => e.Key)
                .ToList();

            var entitiesWithCrudControllers = model.Entities
                .Where(e => e.Value.GenerateCrudControllers)
                .Select(e => e.Key)
                .ToList();

            // Generate Web API project
            var webApiResult = _webApiProjectGenerator.Execute(
                @"C:\MUNI\diplomka\dbmlgentest\gen",
                model.Name,
                "Host=localhost;Database=shop_db;Username=postgres;Password=postgres",
                entitiesWithCrudRepos,
                entitiesWithCrudControllers
            );

            if (webApiResult.IsFailed)
            {
                return Result.Fail(webApiResult.Errors);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during workflow execution.");
            return Result.Fail(ex.Message);
        }
        finally
        {
            if (!string.IsNullOrEmpty(databaseDockerContainerName))
            {
                _logger.LogInformation(
                    "Shutting down and removing Docker container{newline}ContainerName: '{containerName}'",
                    Environment.NewLine,
                    databaseDockerContainerName);
                // Stop Docker container
                _commandRunner.RunAndCheck("docker", $"stop {databaseDockerContainerName}");

                // Remove Docker container
                _commandRunner.RunAndCheck("docker", $"rm {databaseDockerContainerName}");
                _logger.LogInformation("Workflow completed");
            }
        }
    }
}
