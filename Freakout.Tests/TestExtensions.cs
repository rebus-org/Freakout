using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Freakout.Tests;

public static class TestExtensions
{
    public static async Task RunBackgroundWorkersAsync(this ServiceProvider serviceProvider, CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var servicesToStop = new ConcurrentStack<IHostedService>();

        try
        {
            var hostedServices = serviceProvider.GetServices<IHostedService>();

            foreach (var service in hostedServices)
            {
                await service.StartAsync(stoppingToken);
                servicesToStop.Push(service);
            }

            await Task.Delay(-1, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // it's ok
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Failed to run background services: {exception}");
        }
        finally
        {
            while (servicesToStop.TryPop(out var service))
            {
                await service.StopAsync(CancellationToken.None);
            }
        }
    }
}