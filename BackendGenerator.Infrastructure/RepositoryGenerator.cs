using BackendGenerator.Core.Models.hyml;
using System.IO.Abstractions;

namespace BackendGenerator.Infrastructure;

public class RepositoryGenerator
{
    private readonly IFileSystem _fileSystem;
    public RepositoryGenerator(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    public void EmitRepositories(IEnumerable<string?>? entityNames, string outputDir, string template, string projectName, string dbContextName)
    {

        if(entityNames is null || entityNames.Any() is false)
        {
            throw new ArgumentNullException("List of database entities cannot be null or empty");
        }

        if (!_fileSystem.Directory.Exists(outputDir))
        {
            _fileSystem.Directory.CreateDirectory(outputDir);
        }

        foreach (var entity in entityNames)
        {
            var repoCode = template
                .Replace("{{ENTITY}}", entity)
                .Replace("{{NAMESPACE}}", $"{projectName}.Repositories")
                .Replace("{{PROJECTNAME}}", projectName)
                .Replace("{{DATABASECONTEXT}}", dbContextName);
            _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(outputDir, $"{entity}Repository.cs"), repoCode);
        }
    }

    public void EmitRepositoryInterface(string outputDir, string template, string projectName)
    {
        if (!_fileSystem.Directory.Exists(outputDir))
        {
            _fileSystem.Directory.CreateDirectory(outputDir);
        }

        var repoInterface = template.Replace("{{NAMESPACE}}", $"{projectName}.Repositories");
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(outputDir, $"IRepository.cs"), repoInterface);
    }
}
