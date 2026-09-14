using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Timer = System.Timers.Timer;
// ReSharper disable UseAwaitUsing

namespace Freakout.Internals;

class FreakoutBackgroundService(FreakoutConfiguration configuration, IBatchDispatcher dispatcher, IOutboxCommandStore store, ILogger<FreakoutBackgroundService> logger) : BackgroundService
{
    readonly AsyncAutoResetEvent _autoResetEvent = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var _ = stoppingToken.Register(() => logger.LogInformation("Detected stop signal"));

        logger.LogInformation("Starting Freakout background worker with {interval} poll interval", configuration.OutboxPollInterval);

        using var timer = new Timer(configuration.OutboxPollInterval.TotalMilliseconds);

        timer.Elapsed += (_, _) =>
        {
            logger.LogDebug("Triggering store poll");
            _autoResetEvent.Set();
        };
        timer.Start();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _autoResetEvent.WaitAsync(stoppingToken);

                try
                {
                    logger.LogDebug("Polling store");

                    using var batch = await store.GetPendingOutboxCommandsAsync(configuration.CommandProcessingBatchSize, stoppingToken);

                    // if there were no commands, quit processing this time around
                    if (!batch.Any())
                    {
                        continue;
                    }

                    using var scope = new FreakoutContextScope(batch.FreakoutContext);

                    await dispatcher.ExecuteAsync(batch, stoppingToken);

                    await batch.CompleteAsync(stoppingToken);

                    // if we're running in continuous mode, signal that we must poll again
                    if (configuration.ContinuousCommandProcessing)
                    {
                        _autoResetEvent.Set();
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // don't catch this
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Error when executing store commands");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // great
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled error in Frekout background worker");
        }
        finally
        {
            logger.LogInformation("Freakout background worker stopped");
        }
    }
}