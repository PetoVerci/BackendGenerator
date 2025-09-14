using FluentResults;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackendGenerator.Infrastructure.Utils;

public static class ResultLoggingExtensions
{
    /// <summary>
    /// Logs all errors in the result and returns a failed Result if applicable.
    /// </summary>
    public static Result<T> LogAndReturnIfFailed<T>(this Result<T> result, ILogger logger, string contextMessage)
    {
        if (result.IsFailed)
        {
            var errors = string.Join(Environment.NewLine, result.Errors.Select(e => e.Message));
            logger.LogError("{Context}: {Errors}", contextMessage, errors);
        }

        return result;
    }

    /// <summary>
    /// Logs all errors in the result and returns a failed Result if applicable (non-generic version).
    /// </summary>
    public static Result LogAndReturnIfFailed(this Result result, ILogger logger, string contextMessage)
    {
        if (result.IsFailed)
        {
            var errors = string.Join(Environment.NewLine, result.Errors.Select(e => e.Message));
            logger.LogError("{Context}: {Errors}", contextMessage, errors);
        }

        return result;
    }
}
