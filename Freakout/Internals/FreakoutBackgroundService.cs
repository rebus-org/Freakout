using System;
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

                    await dispatcher.ExecuteAsync(batch, stoppingToken);

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