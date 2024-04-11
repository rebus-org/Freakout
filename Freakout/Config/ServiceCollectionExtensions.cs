using System;
using System.Linq;
using Freakout.Internals;
using Freakout.Internals.Dispatch;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable SimplifyLinqExpressionUseAll

namespace Freakout.Config;

public static class ServiceCollectionExtensions
{
    public static void AddFreakout(this IServiceCollection services, FreakoutConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        services.AddSingleton(configuration);

        services.AddHostedService(p => new FreakoutBackgroundService(
            configuration: configuration,
            freakoutDispatcher: p.GetRequiredService<FreakoutDispatcher>(),
            outbox: p.GetRequiredService<IOutbox>(),
            logger: p.GetLoggerFor<FreakoutBackgroundService>()
        ));

        services.AddSingleton(configuration.CommandSerializer);

        // this is special and a monster hack: Stuff the command serializer in the background too!
        Globals.Set(configuration.CommandSerializer);

        services.AddSingleton<FreakoutDispatcher>();

        // let the concrete configuration make its registrations
        configuration.ConfigureServices(services);

        // must have IOutbox implementation now
        if (!services.Any(s => s.ServiceType == typeof(IOutbox)))
        {
            throw new ApplicationException($"The configuration {configuration} did not make the necessary IOutbox registration");
        }
    }

    /// <summary>
    /// Adds the type <typeparamref name="TCommandHandler"/> as a Freakout command handler for commands of type <typeparamref name="TCommand"/>.
    /// It is required that <typeparamref name="TCommandHandler"/> implements <see cref="ICommandHandler{TCommand}"/> closed with a compatible type.
    /// </summary>
    public static void AddCommandHandler<TCommand, TCommandHandler>(this IServiceCollection services) where TCommandHandler : class, ICommandHandler<TCommand>
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        
        services.AddScoped<ICommandHandler<TCommand>, TCommandHandler>();
    }

}