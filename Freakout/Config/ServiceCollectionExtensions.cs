using System;
using System.Linq;
using Freakout.Internals;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable SimplifyLinqExpressionUseAll

namespace Freakout.Config;

public static class ServiceCollectionExtensions
{
    public static void AddFreakout(this IServiceCollection services, FreakoutConfiguration configuration)
    {
        services.AddSingleton(configuration);

        services.AddHostedService(p => new FreakoutBackgroundService(
            configuration: p.GetRequiredService<FreakoutConfiguration>(),
            outbox: p.GetRequiredService<IOutbox>(),
            logger: p.GetLoggerFor<FreakoutBackgroundService>(),
            serviceScopeFactory: p.GetRequiredService<IServiceScopeFactory>()
        ));

        configuration.ConfigureServices(services);

        // must have IOutbox implementation now
        if (!services.Any(s => s.ServiceType == typeof(IOutbox)))
        {
            throw new ApplicationException($"The configuration {configuration} did not make the necessary IOutbox registration");
        }
    }

    public static void AddFreakoutHandler<TCommand, TCommandHandler>(this IServiceCollection services) where TCommandHandler : class, ICommandHandler<TCommand>
    {
        services.AddScoped<ICommandHandler<TCommand>, TCommandHandler>();
    }

}