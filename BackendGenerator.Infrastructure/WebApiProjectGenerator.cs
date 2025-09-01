using FluentResults;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace BackendGenerator.Infrastructure;

public class WebApiProjectGenerator
{
    private readonly CommandRunner _commandRunner;

    public WebApiProjectGenerator(CommandRunner commandRunner)
    {
        _commandRunner = commandRunner;
    }

    public Result Execute()
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), "ShopApi_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempFolder);

        string projectName = "ShopApi";
        string projectPath = Path.Combine(tempFolder, projectName);
        string dbContextName = "ShopDbContext";
        string connectionString = "Host=localhost;Database=Shop;Username=postgres;Password=postgres";



        var result = _commandRunner.RunCommand("dotnet", $"new webapi -o \"{projectPath}\" -n {projectName}");
        Console.WriteLine(result.Output);
        if (result.ExitCode != 0) Console.WriteLine(result.Error);




        var packages = new[]
{
    "Npgsql.EntityFrameworkCore.PostgreSQL",
    "Microsoft.EntityFrameworkCore.Design",
    "Microsoft.EntityFrameworkCore.Tools"
};
        foreach (var pkg in packages)
        {
            var res = _commandRunner.RunCommand("dotnet", $"add \"{projectPath}\" package {pkg}");
            Console.WriteLine(res.Output);
            if (res.ExitCode != 0) Console.WriteLine(res.Error);
        }
        var path = $"{projectPath}\\Models";

        var scaffoldResult = _commandRunner.RunCommand(
            "dotnet",
            $"ef dbcontext scaffold \"{connectionString}\" Npgsql.EntityFrameworkCore.PostgreSQL -o Models -c {dbContextName} --force",
            projectPath // 👈 ensure we're inside the project folder
        );


        Console.WriteLine(scaffoldResult.Output);
        if (scaffoldResult.ExitCode != 0) Console.WriteLine(scaffoldResult.Error);



        string appSettingsPath = Path.Combine(projectPath, "appsettings.json");
        dynamic appSettings = Newtonsoft.Json.JsonConvert.DeserializeObject(File.ReadAllText(appSettingsPath));
        appSettings.ConnectionStrings ??= new Newtonsoft.Json.Linq.JObject();
        appSettings.ConnectionStrings.DefaultConnection = connectionString;
        File.WriteAllText(appSettingsPath, Newtonsoft.Json.JsonConvert.SerializeObject(appSettings, Newtonsoft.Json.Formatting.Indented));

        return Result.Ok();
    }
}
