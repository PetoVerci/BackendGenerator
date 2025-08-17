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
builder.Services.AddTransient<IModelParser, HymlModelParser>();
builder.Services.AddSingleton<ISchemaBuilder, SchemaBuilder>();
builder.Services.AddSingleton<DbmlEmiter>();
builder.Services.AddSingleton<DbmlGenerationExecutor>();
builder.Services.AddSingleton<DbmlFileWriter>();
builder.Services.AddSingleton<CommandRunner>();

var host = builder.Build();

var executor = host.Services.GetRequiredService<DbmlGenerationExecutor>();

executor.Execute("model.yaml");