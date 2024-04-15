using System;
using System.Threading;
using System.Threading.Tasks;
using Freakout.Internals.Dispatch;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Timer = System.Timers.Timer;
// ReSharper disable UseAwaitUsing

namespace Freakout.Internals;

class FreakoutBackgroundService(FreakoutConfiguration configuration, IOutboxCommandDispatcher dispatcher, IOutboxCommandStore store, ILogger<FreakoutBackgroundService> logger) : BackgroundService
{
    readonly AsyncAutoResetEvent AutoResetEvent = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var _ = stoppingToken.Register(() => logger.LogInformation("Detected stop signal"));

        logger.LogInformation("Starting Freakout background worker with {interval} poll interval", configuration.OutboxPollInterval);

        using var timer = new Timer(configuration.OutboxPollInterval.TotalMilliseconds);

        timer.Elapsed += (_, _) =>
        {
            logger.LogDebug("Triggering store poll");
            AutoResetEvent.Set();
        };
        timer.Start();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await AutoResetEvent.WaitAsync(stoppingToken);

                logger.LogDebug("Polling store");

                try
                {
                    using var batch = await store.GetPendingOutboxCommandsAsync(stoppingToken);
                    using var scope = new FreakoutContextScope(batch.FreakoutContext);

                    foreach (var command in batch)
                    {
                        logger.LogDebug("Executing store command {command}", command);

                        try
                        {
                            await dispatcher.ExecuteAsync(command, stoppingToken);

                            logger.LogDebug("Successfully executed store command {command}", command);
                        }
                        catch (Exception exception)
                        {
                            throw new ApplicationException($"Could not execute store command {command}", exception);
                        }
                    }

                    await batch.CompleteAsync(stoppingToken);
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