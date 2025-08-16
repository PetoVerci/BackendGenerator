using BackendGenerator.Core.Interfaces;
using BackendGenerator.Core.Models;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using System.Reflection.Metadata;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static System.Net.Mime.MediaTypeNames;

namespace BackendGenerator.Infrastructure.Parsers;

public class HymlModelParser : IModelParser
{
    private readonly ILogger<HymlModelParser> _logger;
    private readonly IFileSystem _fileSystem;
    public HymlModelParser(ILogger<HymlModelParser> logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

    public Result<Model> Load(string filePath)
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
                .IgnoreUnmatchedProperties()
                .Build();

            var model = deserializer.Deserialize<Model>(yamlContent);

            if (model == null)
            {
                var warningMsg = $"Parsed YAML returned null for file: {filePath}";
                _logger.LogWarning(warningMsg);
                return Result.Fail(warningMsg);
            }


            _logger.LogInformation("YAML file parsed successfully: {FilePath}", filePath);

            return Result.Ok(model);
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
