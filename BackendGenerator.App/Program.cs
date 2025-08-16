using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Abstractions;
using BackendGenerator.Core.Interfaces;
using BackendGenerator.Infrastructure.Parsers;
using BackendGenerator.Infrastructure.Validators;
using BackendGenerator.Infrastructure.Emmiters;



var builder = Host.CreateApplicationBuilder(args);

// Register services (DI)
builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddSingleton<IValidator, HymlModelValidator>();
builder.Services.AddTransient<IModelParser, HymlModelParser>();
builder.Services.AddSingleton<DbmlEmiter>();

var host = builder.Build();

// Resolve service
var parser = host.Services.GetRequiredService<IModelParser>();
var validator = host.Services.GetRequiredService<IValidator>();
var emmiter = host.Services.GetRequiredService<DbmlEmiter>();


var parseResult = parser.Load("model.yaml");


if (parseResult.IsFailed)
{
    Console.WriteLine(parseResult.Errors.ToString());
}
var model = parseResult.Value;
var validation = validator.Validate(model);


var emmit = emmiter.Emit(model);



Console.WriteLine($"Parsed {model.Entities.Count} entities.");
