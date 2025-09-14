using FluentResults;

namespace BackendGenerator.Core.Models;

public record DatabaseExecutionResult(Result CommandResult, string ContainerName);
