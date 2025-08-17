using FluentResults;

namespace BackendGenerator.Core.Interfaces;

public interface IFileWriter
{
    Result CreateFile(string path, string content);
}
