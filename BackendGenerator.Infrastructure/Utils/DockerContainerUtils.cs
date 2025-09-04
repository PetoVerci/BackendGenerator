
using BackendGenerator.Core.Interfaces;
using BackendGenerator.Infrastructure.Exceptions;

namespace BackendGenerator.Infrastructure.Utils;

internal class DockerContainerUtils
{
    internal static void WaitForPostgresReady(string containerName, ICommandRunner commandRunner, int timeoutSeconds = 30, int intervalMs = 500)
    {
        //Added a little time for container to initialize successfully
        Thread.Sleep(3000);
        var start = DateTime.UtcNow;
        while (true)
        {
            var result = commandRunner.RunCommand("docker", $"exec {containerName} pg_isready -U postgres");
            if (result.ExitCode == 0) return;

            if ((DateTime.UtcNow - start).TotalSeconds > timeoutSeconds)
                throw new PostgresNotReadyException($"PostgreSQL container '{containerName}' did not become ready in time.");

            Thread.Sleep(intervalMs);
        }
    }
}
