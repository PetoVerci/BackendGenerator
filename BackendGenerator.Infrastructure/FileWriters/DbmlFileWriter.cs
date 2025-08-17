using BackendGenerator.Core.Interfaces;
using FluentResults;
using System.IO.Abstractions;

namespace BackendGenerator.Infrastructure.FileWriters;

public class DbmlFileWriter : IFileWriter
{
    private readonly IFileSystem _fileSystem;
    public DbmlFileWriter(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public Result CreateFile(string path, string content)
    {
        try
        {
            // Ensure the file path ends with .dbml
            if (_fileSystem.Path.GetExtension(path) != ".dbml")
            {
                return Result.Fail("File must have a .dbml extension.");
            }

            // Ensure the directory exists
            var directory = _fileSystem.Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory) && !_fileSystem.Directory.Exists(directory))
            {
                _fileSystem.Directory.CreateDirectory(directory);
            }

            // Create or overwrite the file
            _fileSystem.File.WriteAllText(path, content);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to create .dbml file '{path}': {ex.Message}");
        }
    }
}
