using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackendGenerator.Infrastructure;

public class ProgramGenerator
{
    private readonly IFileSystem _fileSystem;
    public ProgramGenerator(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    internal void EmitProgramFile(string projectPath, string programTemplate, string projectName, string dbContextName)
    {
        if (!_fileSystem.Directory.Exists(projectPath))
        {
            throw new FileNotFoundException("Program.cs file not found !");
        }

        var programFile = programTemplate
                .Replace("{{PROJECTNAME}}", projectName)
                .Replace("{{DATABASECONTEXT}}", dbContextName);

        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(projectPath, "Program.cs"), programFile);
    }
}
