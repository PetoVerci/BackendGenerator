using BackendGenerator.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BackendGenerator.Infrastructure;

public class CommandRunnerFactory : ICommandRunnerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public CommandRunnerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ICommandRunner Create<TConsumer>()
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<TConsumer>>();
        return new CommandRunner(logger);
    }
}
