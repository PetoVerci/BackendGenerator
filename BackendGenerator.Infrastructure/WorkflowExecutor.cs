using FluentResults;
using Microsoft.Extensions.Logging;

namespace BackendGenerator.Infrastructure;

public class WorkflowExecutor
{
    private readonly PersistanceLayerGenerationExecutor _persistanceLayerGenerator;
    private readonly WebApiProjectGenerator _webApiProjectGenerator;
    private readonly ILogger<WorkflowExecutor> _logger;
    public WorkflowExecutor(PersistanceLayerGenerationExecutor persistanceLayerGenerator, WebApiProjectGenerator webApiProjectGenerator, ILogger<WorkflowExecutor> logger)
    {
        _persistanceLayerGenerator = persistanceLayerGenerator;
        _webApiProjectGenerator = webApiProjectGenerator;
        _logger = logger;
    }

    public void Execute(string pathToModel)
    {
        Result<string> databaseGenerationResult = _persistanceLayerGenerator.Execute(pathToModel);

        if (databaseGenerationResult.IsFailed)
        {
            _logger.LogError("Persistance layer generation failed. Errors : {errors}", databaseGenerationResult.Errors);
        }

        _webApiProjectGenerator.Execute("C:\\MUNI\\diplomka\\dbmlgentest\\gen", "Shop", "Host=localhost;Database=shop_db;Username=postgres;Password=postgres");
    }
}
