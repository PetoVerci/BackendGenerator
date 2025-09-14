using BackendGenerator.Core.Interfaces;
using BackendGenerator.Core.Models.hyml;
using BackendGenerator.Core.Models.Schema;
using BackendGenerator.Infrastructure.Emmiters;
using BackendGenerator.Infrastructure.FileWriters;
using BackendGenerator.Infrastructure.Utils;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;

namespace BackendGenerator.Infrastructure;

public class PersistanceLayerGenerationExecutor
{
    private readonly ISchemaBuilder _schemaBuilder;
    private readonly CommandRunner _commandRunner;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<PersistanceLayerGenerationExecutor> _logger;
    private readonly SqlFileWriter _sqlFileWriter;
    public PersistanceLayerGenerationExecutor(ISchemaBuilder schemaBuilder,ILogger<PersistanceLayerGenerationExecutor> logger,
        CommandRunner commandRunner, IFileSystem fileSystem, SqlFileWriter sqlFileWriter)
    {
        _schemaBuilder = schemaBuilder;
        _logger = logger;
        _commandRunner = commandRunner;
        _fileSystem = fileSystem;
        _sqlFileWriter = sqlFileWriter;
    }

    //TODO : Figure out how to enable this to run on linux aswell -> make this app OS agnostic
    //TODO : How to bundle the dbml2sql binaries into the packed application when published 

    public Result<string> Execute(HymlModel model)
    {
        //retrieve temp path for creating db files
        string tempPath = _fileSystem.Path.GetTempPath() ?? string.Empty;

        //persist sql file
        TableRegistry schema = _schemaBuilder.Build(model);

        var emittedSql = SqlEmitter.Emit(schema);
        string sqlOutputFilePath = _fileSystem.Path.Combine(tempPath, $"{model.Name}.sql");

        Result sqlWritten = _sqlFileWriter.WriteToFile(sqlOutputFilePath, emittedSql);

        //assign .sql file path
        string sqlOutputPath = _fileSystem.Path.Combine(tempPath, $"{model.Name}.sql");
        string databaseName = $"{model.Name.ToLower()}_db";
        string dockerContainerName = $"{model.Name}-db";


        // Start PostgreSQL container
        _commandRunner.RunCommand(
            "docker",
            $"run --name {dockerContainerName} -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB={databaseName} -p 5432:5432 -d postgres"
        );

        // Copy SQL file into container
        _commandRunner.RunCommand(
            "docker",
            $"cp \"{sqlOutputPath}\" {dockerContainerName}:/tmp/{model.Name}.sql"
        );

        DockerContainerUtils.WaitForPostgresReady(dockerContainerName, _commandRunner);

        // Execute SQL inside container
        _commandRunner.RunCommand(
            "docker",
            $"exec -i {dockerContainerName} psql -U postgres -d {databaseName} -f /tmp/{model.Name}.sql"
        );

        return Result.Ok($"{dockerContainerName}");
    }
}
