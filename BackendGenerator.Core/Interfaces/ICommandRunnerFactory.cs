namespace BackendGenerator.Core.Interfaces;

public interface ICommandRunnerFactory
{
    ICommandRunner Create<TConsumer>();
}

