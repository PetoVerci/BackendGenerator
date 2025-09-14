using BackendGenerator.Core.Interfaces;
using BackendGenerator.Infrastructure;
using BackendGenerator.Infrastructure.Emmiters;
using BackendGenerator.Infrastructure.FileWriters;
using BackendGenerator.Infrastructure.Parsers;
using BackendGenerator.Infrastructure.Schema;
using BackendGenerator.Infrastructure.Validators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO.Abstractions;


var builder = Host.CreateApplicationBuilder(args);

// Register services (DI)
builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddSingleton<IModelValidator, HymlModelValidator>();
builder.Services.AddTransient<HymlModelParser>();
builder.Services.AddSingleton<ISchemaBuilder, SchemaBuilder>();
builder.Services.AddSingleton<PersistanceLayerGenerationExecutor>();
builder.Services.AddSingleton<WebApiProjectGenerator>();
builder.Services.AddSingleton<RepositoryGenerator>();
builder.Services.AddSingleton<SqlFileWriter>();
builder.Services.AddSingleton<AppSettingsDbConnectionWriter>();
builder.Services.AddSingleton<WorkflowExecutor>();
builder.Services.AddSingleton<CommandRunner>();
builder.Services.AddSingleton<ProgramGenerator>();

var host = builder.Build();

var executor = host.Services.GetRequiredService<WorkflowExecutor>();

var baseDir = AppContext.BaseDirectory;

var pathToFile = Path.Combine(baseDir, "model.yaml");

executor.Execute(pathToFile);