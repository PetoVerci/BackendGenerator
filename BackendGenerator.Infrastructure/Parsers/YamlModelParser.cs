using BackendGenerator.Core.Interfaces;
using BackendGenerator.Core.Models;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

namespace BackendGenerator.Infrastructure.Parsers;

public class YamlModelParser : IModelParser
{
    private readonly ILogger<YamlModelParser> _logger;
    private readonly IFileSystem _fileSystem;
    public YamlModelParser(ILogger<YamlModelParser> logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }
    public Result<ParsedModelDefinition> Parse(string filePath)
    {
        if (!_fileSystem.File.Exists(filePath))
        {
            var msg = $"YAML file not found: {filePath}";
            _logger.LogError(msg);
            return Result.Fail(msg);
        }

        try
        {
            var yamlContent = _fileSystem.File.ReadAllText(filePath);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            ParsedModelDefinition modelDefinition = deserializer.Deserialize<ParsedModelDefinition>(yamlContent);

            if (modelDefinition == null)
            {
                var warningMsg = $"Parsed YAML returned null for file: {filePath}";
                _logger.LogWarning(warningMsg);
                return Result.Fail(warningMsg);
            }

            if (modelDefinition.Entities == null || !modelDefinition.Entities.Any())
            {
                var warningMsg = $"Parsed YAML contains no entities in file: {filePath}";
                _logger.LogWarning(warningMsg);
                // You might want to treat empty entities as failure or success depending on your logic
                // For now, I treat as failure:
                return Result.Fail(warningMsg);
            }

            // Build ParsedModelDefinition from ModelDefinition
            ParsedModelDefinition parsedModel = new ParsedModelDefinition
            {
                Entities = modelDefinition.Entities,
                Queries = modelDefinition.Queries ?? new List<QueryDefinition>()
            };

            _logger.LogInformation("YAML file parsed successfully: {FilePath}", filePath);

            return Result.Ok(parsedModel);
        }
        catch (YamlDotNet.Core.YamlException ye)
        {
            var line = ye.Start.Line;  // 1-based line number where error occurred
            var column = ye.Start.Column;
            var msg = $"YAML parse error at line {line}, column {column}: {ye.Message}";
            _logger.LogError(ye, msg);
            return Result.Fail(msg);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error parsing YAML file {filePath}: {ex.Message}, innerException : {ex.InnerException?.Message}";
            _logger.LogError(ex, errorMsg);
            return Result.Fail(errorMsg).WithError(ex.ToString());
        }
    }

}
