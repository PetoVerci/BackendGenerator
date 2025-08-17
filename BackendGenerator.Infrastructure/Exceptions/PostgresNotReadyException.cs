namespace BackendGenerator.Infrastructure.Exceptions;

public class PostgresNotReadyException : Exception
{
    public PostgresNotReadyException(string message) : base(message) { }
}

