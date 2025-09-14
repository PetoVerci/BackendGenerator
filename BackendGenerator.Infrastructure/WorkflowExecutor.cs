using BackendGenerator.Core.Interfaces;
using BackendGenerator.Core.Models.hyml;
using BackendGenerator.Infrastructure.Parsers;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace BackendGenerator.Infrastructure;

public class WorkflowExecutor
{
    private readonly PersistanceLayerGenerationExecutor _persistanceLayerGenerator;
    private readonly WebApiProjectGenerator _webApiProjectGenerator;
    private readonly ILogger<WorkflowExecutor> _logger;
    private readonly HymlModelParser _hymlModelParser;
    private readonly CommandRunner _commandRunner;
    public WorkflowExecutor(PersistanceLayerGenerationExecutor persistanceLayerGenerator, WebApiProjectGenerator webApiProjectGenerator,
        ILogger<WorkflowExecutor> logger, HymlModelParser hymlModelParser, CommandRunner commandRunner)
    {
        _persistanceLayerGenerator = persistanceLayerGenerator;
        _webApiProjectGenerator = webApiProjectGenerator;
        _logger = logger;
        _hymlModelParser = hymlModelParser;
        _commandRunner = commandRunner;
    }

    public Result<string> Execute(string pathToModel)
    {
        //parse the model file
        Result<HymlModel> parseResult = _hymlModelParser.Load(pathToModel);
        if (parseResult.IsFailed)
        {
            var errors = parseResult.Errors.ToString();
            _logger.LogError(errors);
            return Result.Fail($"Failed while parsing model. errors : {errors}");
        }
        HymlModel model = parseResult.Value;

        Result<string> databaseGenerationResult = _persistanceLayerGenerator.Execute(model);

        if (databaseGenerationResult.IsFailed)
        {
            _logger.LogError("Persistance layer generation failed. Errors : {errors}", databaseGenerationResult.Errors);
        }

        string databaseDockerContainerName = databaseGenerationResult.Value;

        List<string> entitiesWithCrudRepos = model.Entities.Where(e => e.Value.GenerateCrudRepositories is true).Select(e => e.Key).ToList();
        List<string> entitiesWithCrudControllers = model.Entities.Where(e => e.Value.GenerateCrudControllers is true).Select(e => e.Key).ToList();

        _webApiProjectGenerator.Execute("C:\\MUNI\\diplomka\\dbmlgentest\\gen", model.Name, "Host=localhost;Database=shop_db;Username=postgres;Password=postgres", entitiesWithCrudRepos, entitiesWithCrudControllers);

        _commandRunner.RunCommand("docker", $"stop {databaseDockerContainerName}");
        _commandRunner.RunCommand("docker", $"rm {databaseDockerContainerName}");

        return Result.Ok("New web api project created successfully");
    }
}
