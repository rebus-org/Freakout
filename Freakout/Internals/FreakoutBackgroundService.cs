using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Timer = System.Timers.Timer;

namespace Freakout.Internals;

class FreakoutBackgroundService(FreakoutConfiguration configuration, IOutbox outbox, ILogger<FreakoutBackgroundService> logger, IServiceScopeFactory serviceScopeFactory) : BackgroundService
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
                    var batch = await outbox.GetPendingOutboxCommandsAsync(stoppingToken);

                    foreach (var command in batch)
                    {
                        logger.LogDebug("Executing outbox command {command}", command);

                        try
                        {
                            await ExecuteOutboxCommand(command, stoppingToken);

                            logger.LogDebug("Successfully executed outbox command {command}", command);
                        }
                        catch (Exception exception)
                        {
                            throw new ApplicationException($"Could not execute outbox command {command}", exception);
                        }
                    }
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Error when executing outbox commands");
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

    async Task ExecuteOutboxCommand(OutboxCommand command, CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var type = Type.GetType(command.Headers[HeaderKeys.Type]);

        var commandHandlerType = typeof(ICommandHandler<>).MakeGenericType(type);
        dynamic handler = scope.ServiceProvider.GetRequiredService(commandHandlerType);

        var commandObject = JsonSerializer.Deserialize(command.Payload, type);
        await (Task)handler.HandleAsync(commandObject, cancellationToken);
    }
}