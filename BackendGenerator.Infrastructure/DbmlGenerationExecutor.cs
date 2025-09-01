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

public class DbmlGenerationExecutor
{
    private readonly IModelParser _modelParser;
    private readonly ISchemaBuilder _schemaBuilder;
    private readonly DbmlEmiter _dbmlEmiter;
    private readonly DbmlFileWriter _writer;
    private readonly CommandRunner _commandRunner;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<DbmlGenerationExecutor> _logger;
    public DbmlGenerationExecutor(IModelParser modelParser, ISchemaBuilder schemaBuilder, DbmlEmiter dbmlEmiter,
        ILogger<DbmlGenerationExecutor> logger, DbmlFileWriter writer, CommandRunner commandRunner, IFileSystem fileSystem)
    {
        _modelParser = modelParser;
        _schemaBuilder = schemaBuilder;
        _dbmlEmiter = dbmlEmiter;
        _logger = logger;
        _writer = writer;
        _commandRunner = commandRunner;
        _fileSystem = fileSystem;
    }

    //TODO : Figure out how to enable this to run on linux aswell -> make this app OS agnostic
    //TODO : How to bundle the dbml2sql binaries into the packed application when published 

    public Result<string> Execute(string modelFilePath)
    {
        //retrieve temp path for creating db files
        string tempPath = _fileSystem.Path.GetTempPath() ?? string.Empty;

        //parse the model file
        Result<HymlModel> parseResult = _modelParser.Load(modelFilePath);
        if (parseResult.IsFailed)
        {
            var errors = parseResult.Errors.ToString();
            _logger.LogError(errors);
            return Result.Fail($"Failed while parsing model. errors : {errors}");
        }
        HymlModel model = parseResult.Value;


        //retrieve dbml file path
        string dbmlOutputFilePath = _fileSystem.Path.Combine(tempPath, $"{model.Name}.dbml");


        //persiste dbml file
        TableRegistry schema = _schemaBuilder.Build(model);
        string dbmlSchema = _dbmlEmiter.Emit(schema);
        Result schemaWritten = _writer.CreateFile(dbmlOutputFilePath, dbmlSchema);


        if (schemaWritten.IsFailed)
        {
            return Result.Fail(string.Join(", ", schemaWritten.Errors.Select(e => e.Message)));
        }


        //assign .sql file path
        string sqlOutputPath = _fileSystem.Path.Combine(tempPath, $"{model.Name}.sql");

        // Generate SQL from DBML
        _commandRunner.RunCommand(
            "dbml2sql.cmd",
            $"\"{dbmlOutputFilePath}\" -o \"{sqlOutputPath}\" -t postgres"
        );

        // Start PostgreSQL container
        _commandRunner.RunCommand(
            "docker",
            $"run --name {model.Name}-db -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB={model.Name} -p 5432:5432 -d postgres"
        );

        // Copy SQL file into container
        _commandRunner.RunCommand(
            "docker",
            $"cp \"{sqlOutputPath}\" {model.Name}-db:/tmp/{model.Name}.sql"
        );

        DockerContainerUtils.WaitForPostgresReady(model.Name, _commandRunner);

        // Execute SQL inside container
        _commandRunner.RunCommand(
            "docker",
            $"exec -i {model.Name}-db psql -U postgres -d {model.Name} -f /tmp/{model.Name}.sql"
        );


        WebApiProjectGenerator webApiGenerator = new(_commandRunner);

        webApiGenerator.Execute();

        return Result.Ok($"Schema with {dbmlSchema}");
    }
}
