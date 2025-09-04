using BackendGenerator.Core.Interfaces;
using FluentResults;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackendGenerator.Infrastructure.FileWriters
{

    public class AppSettingsDbConnectionWriter : IFileWriter
    {
        private readonly IFileSystem _fileSystem;

        public AppSettingsDbConnectionWriter(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public Result WriteToFile(string appSettingsPath, string content)
        {
            try
            {
                dynamic appSettings = Newtonsoft.Json.JsonConvert.DeserializeObject(_fileSystem.File.ReadAllText(appSettingsPath));
                appSettings.ConnectionStrings ??= new Newtonsoft.Json.Linq.JObject();
                appSettings.ConnectionStrings.DefaultConnection = content;
                _fileSystem.File.WriteAllText(appSettingsPath, Newtonsoft.Json.JsonConvert.SerializeObject(appSettings, Newtonsoft.Json.Formatting.Indented));
                return Result.Ok();
            }
            catch { return Result.Fail("Failed to write database connection string to appsettings file"); }
        }
    }
}
