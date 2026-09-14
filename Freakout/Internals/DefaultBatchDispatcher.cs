using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Freakout.Internals;

class DefaultBatchDispatcher(FreakoutConfiguration configuration, ICommandDispatcher commandDispatcher, ILogger<DefaultBatchDispatcher> logger) : IBatchDispatcher
{
    public async Task ExecuteAsync(OutboxCommandBatch batch, CancellationToken cancellationToken = default)
    {
        foreach (var command in batch)
        {
            var stopwatch = Stopwatch.StartNew();

            logger.LogDebug("Executing store command {command}", command);

            try
            {
                using var timeout = new CancellationTokenSource(configuration.CommandProcessingTimeout);
                using var combined = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);

                var token = combined.Token;

                await commandDispatcher.ExecuteAsync(command, token);

                command.SetState(new SuccessfullyExecutedCommandState(stopwatch.Elapsed));

                logger.LogDebug("Successfully executed command with ID {commandId}", command.Id);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // just let it bubble out
                throw;
            }
            catch (Exception exception)
            {
                command.SetState(new FailedCommandState(stopwatch.Elapsed, exception));

                throw new ApplicationException($"Could not execute command with ID {command.Id}", exception);
            }
        }
    }
}