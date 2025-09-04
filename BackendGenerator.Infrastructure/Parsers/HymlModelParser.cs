using BackendGenerator.Core.Interfaces;
using BackendGenerator.Core.Models.hyml;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BackendGenerator.Infrastructure.Parsers;

public class HymlModelParser : IModelParser
{
    private readonly ILogger<HymlModelParser> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IModelValidator _validator;
    public HymlModelParser(ILogger<HymlModelParser> logger, IFileSystem fileSystem, IModelValidator validator)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _validator = validator;
    }

    public Result<HymlModel> Load(string filePath)
    {
        if (!_fileSystem.File.Exists(filePath))
        {
            var msg = $"HYML file not found: {filePath}";
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

            var model = deserializer.Deserialize<HymlModel>(yamlContent);

            if (model == null)
            {
                var warningMsg = $"Parsed HYML returned null for file: {filePath}";
                _logger.LogWarning(warningMsg);
                return Result.Fail(warningMsg);
            }


            _logger.LogInformation("HYML file parsed successfully: {FilePath}", filePath);

            var validationErrors = _validator.Validate(model);
            if (validationErrors.Count > 0)
            {
                foreach (var err in validationErrors)
                    _logger.LogError("Validation error: {Error}", err);

                return Result.Fail(
                    $"Validation failed for model '{model.Name ?? "unnamed"}' " +
                    $"with {validationErrors.Count} error(s)."
                ).WithErrors(validationErrors.Select(e => new Error(e)));
            }

            return Result.Ok(model);
        }

        catch (YamlException ye)
        {
            var line = ye.Start.Line;  // 1-based line number where error occurred
            var column = ye.Start.Column;
            var msg = $"HYML parse error at line {line}, column {column}: {ye.Message}";
            _logger.LogError(ye, msg);
            return Result.Fail(msg);
        }

        catch (Exception ex)
        {
            var errorMsg = $"Error parsing HYML file {filePath}: {ex.Message}, innerException : {ex.InnerException?.Message}";
            _logger.LogError(ex, errorMsg);
            return Result.Fail(errorMsg).WithError(ex.ToString());
        }
    }
}
