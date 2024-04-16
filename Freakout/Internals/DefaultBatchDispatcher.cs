using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Freakout.Internals;

class DefaultBatchDispatcher(ICommandDispatcher commandDispatcher, ILogger<DefaultBatchDispatcher> logger) : IBatchDispatcher
{
    public async Task ExecuteAsync(OutboxCommandBatch batch, CancellationToken cancellationToken)
    {
        foreach (var command in batch)
        {
            var stopwatch = Stopwatch.StartNew();

            logger.LogDebug("Executing store command {command}", command);

            try
            {
                await commandDispatcher.ExecuteAsync(command, cancellationToken);

                command.SetState(new SuccessfullyExecutedCommandState(stopwatch.Elapsed));

                logger.LogDebug("Successfully executed store command {command}", command);
            }
            catch (Exception exception)
            {
                command.SetState(new FailedCommandState(stopwatch.Elapsed, exception));

                throw new ApplicationException($"Could not execute command {command}", exception);
            }
        }
    }
}