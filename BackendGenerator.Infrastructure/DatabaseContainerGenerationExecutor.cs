using BackendGenerator.Core.Interfaces;
using BackendGenerator.Core.Models;
using BackendGenerator.Core.Models.hyml;
using BackendGenerator.Core.Models.Schema;
using BackendGenerator.Infrastructure.Emmiters;
using BackendGenerator.Infrastructure.FileWriters;
using BackendGenerator.Infrastructure.Utils;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;

namespace BackendGenerator.Infrastructure;

public class DatabaseContainerGenerationExecutor
{
    private readonly ISchemaBuilder _schemaBuilder;
    private readonly ICommandRunner _commandRunner;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<DatabaseContainerGenerationExecutor> _logger;
    private readonly SqlFileWriter _sqlFileWriter;
    public DatabaseContainerGenerationExecutor(ISchemaBuilder schemaBuilder, ILogger<DatabaseContainerGenerationExecutor> logger,
        IFileSystem fileSystem, SqlFileWriter sqlFileWriter, ICommandRunnerFactory commandRunneFactory)
    {
        _schemaBuilder = schemaBuilder;
        _logger = logger;
        _fileSystem = fileSystem;
        _sqlFileWriter = sqlFileWriter;
        _commandRunner = commandRunneFactory.Create<DatabaseContainerGenerationExecutor>();
    }

    //TODO : Figure out how to enable this to run on linux aswell -> make this app OS agnostic
    //TODO : How to bundle the dbml2sql binaries into the packed application when published 

    public DatabaseExecutionResult Execute(HymlModel model)
    {
        _logger.LogInformation("Starting database creation");

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
        var startDockerContainerResult = _commandRunner.RunAndCheck(
            "docker",
            $"run --name {dockerContainerName} -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB={databaseName} -p 5432:5432 -d postgres"
        );
        if (startDockerContainerResult.IsFailed)
        {
            return new DatabaseExecutionResult(Result.Fail(startDockerContainerResult.Errors), dockerContainerName);
        }

        // Copy SQL file into container
        Result<CommandResult> copyingSqlResult = _commandRunner.RunAndCheck(
            "docker",
            $"cp \"{sqlOutputPath}\" {dockerContainerName}:/tmp/{model.Name}.sql"
        );

        if (copyingSqlResult.IsFailed)
        {
            return new DatabaseExecutionResult(Result.Fail(copyingSqlResult.Errors), dockerContainerName);
        }

        DockerContainerUtils.WaitForPostgresReady(dockerContainerName, _commandRunner);

        // Execute SQL inside container
        Result<CommandResult> sqlExecutionResult = _commandRunner.RunAndCheck(
            "docker",
            $"exec -i {dockerContainerName} psql -U postgres -d {databaseName} -f /tmp/{model.Name}.sql"
        );

        if (sqlExecutionResult.IsFailed)
        {
            return new DatabaseExecutionResult(Result.Fail(sqlExecutionResult.Errors), dockerContainerName);
        }
        _logger.LogInformation(
                "Docker container created successfully with applied sql create script{newline}ContainerName: '{containername}'",
                Environment.NewLine,
                dockerContainerName);

        return new DatabaseExecutionResult(Result.Ok(), dockerContainerName);
    }
}
