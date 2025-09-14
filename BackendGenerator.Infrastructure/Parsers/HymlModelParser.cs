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
            _logger.LogError(
                "HYML file not found.{newline}FilePath: '{filePath}'",
                Environment.NewLine,
                filePath
            );
            return Result.Fail($"HYML file not found: {filePath}");
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
                _logger.LogWarning(
                    "Parsed HYML returned null. {newline}Filepath: '{filePath}'",
                    Environment.NewLine,
                    filePath);
                return Result.Fail($"Parsed HYML returned null for file: {filePath}");
            }


            _logger.LogInformation(
                "HYML file parsed successfully{newline}FilePath: '{filePath}'",
                Environment.NewLine,
                filePath);

            var validationErrors = _validator.Validate(model);
            if (validationErrors.Count > 0)
            {
                foreach (var err in validationErrors)
                {
                    _logger.LogError("HYML file validation error: '{Error}'", err);
                }

                return Result.Fail(
                    $"Validation failed for model '{model.Name ?? "unnamed"}' " +
                    $"with {validationErrors.Count} error(s)."
                ).WithErrors(validationErrors.Select(e => new Error(e)));
            }

            return Result.Ok(model);
        }

        catch (YamlException ye)
        {
            long line = ye.Start.Line;  // 1-based line number where error occurred
            long column = ye.Start.Column;
            string msg = $"HYML parse error at Line '{line}', Column '{column}', Exception message: '{ye.Message}', Inner exception : '{ye.InnerException?.Message}'";
            _logger.LogError(
                ye,
                "HYML parse error at Line '{line}', Column '{column}', Exception message: '{Message}', Inner exception : '{InnerException}'",
                line,
                column,
                ye.Message,
                ye.InnerException?.Message);
            return Result.Fail(msg);
        }

        catch (Exception ex)
        {
            var errorMsg = $"Error parsing HYML File with filepath: {filePath}, Excetion message: {ex.Message}, Inner exception : {ex.InnerException?.Message}";
            _logger.LogError(
                ex,
                "Error parsing HYML File with filepath: '{filePath}', Excetion message: '{Message}', Inner exception : '{InnerExceptione}'",
                filePath,
                ex.Message,
                ex.InnerException?.Message);
            return Result.Fail(errorMsg).WithError(ex.ToString());
        }
    }
}
