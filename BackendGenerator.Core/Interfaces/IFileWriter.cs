using FluentResults;

namespace BackendGenerator.Core.Interfaces;

public interface IFileWriter
{
    Result WriteToFile(string path, string content);
}
