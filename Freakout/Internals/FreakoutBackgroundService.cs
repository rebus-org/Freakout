using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Timer = System.Timers.Timer;

namespace Freakout.Internals;

class FreakoutBackgroundService(FreakoutConfiguration configuration, IOutbox outbox, ILogger<FreakoutBackgroundService> logger) : BackgroundService
{
    readonly AsyncAutoResetEvent AutoResetEvent = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var _ = stoppingToken.Register(() => logger.LogInformation("Detected stop signal"));

        logger.LogInformation("Starting Freakout background worker with {interval} poll interval", configuration.OutboxPollInterval);

        using var timer = new Timer(configuration.OutboxPollInterval.TotalMilliseconds);

        timer.Elapsed += (_, _) =>
        {
            logger.LogDebug("Triggering outbox poll");
            AutoResetEvent.Set();
        };
        timer.Start();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await AutoResetEvent.WaitAsync(stoppingToken);

                logger.LogDebug("Polling outbox");

                try
                {
                    var tasks = await outbox.GetPendingOutboxTasksAsync(stoppingToken);

                    foreach (var task in tasks)
                    {
                        logger.LogDebug("Executing outbox task {task}", task);

                        try
                        {
                            await task.ExecuteAsync();
                            
                            logger.LogDebug("Successfully executed outbox task {task}", task);
                        }
                        catch (Exception exception)
                        {
                            throw new ApplicationException($"Could not execute outbox task {task}", exception);
                        }
                    }
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Error when executing outbox tasks");
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