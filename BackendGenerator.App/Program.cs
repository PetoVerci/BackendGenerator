using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Abstractions;
using BackendGenerator.Core.Interfaces;
using BackendGenerator.Infrastructure.Parsers;



var builder = Host.CreateApplicationBuilder(args);

// Register services (DI)
builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddTransient<IModelParser, YamlModelParser>();

var host = builder.Build();

// Resolve service
var parser = host.Services.GetRequiredService<IModelParser>();

var parseResult = parser.Parse("model.yaml");

if (parseResult.IsFailed)
{
    Console.WriteLine(parseResult.Errors.ToString());
}
var model = parseResult.Value;



Console.WriteLine($"Parsed {model.Entities.Count} entities.");
