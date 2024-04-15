using System;
using System.Linq;
using Freakout.Internals;
using Freakout.Internals.Dispatch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable SimplifyLinqExpressionUseAll

namespace Freakout.Config;

/// <summary>
/// Configuration extensions for Freakout
/// </summary>
public static class FreakoutServiceCollectionExtensions
{
    /// <summary>
    /// Adds Freakout to the container, using the given <paramref name="configuration"/>.
    /// </summary>
    public static void AddFreakout(this IServiceCollection services, FreakoutConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        if (Globals.Get<FreakoutConfiguration>() != null)
        {
            throw new InvalidOperationException(
                $"Found global FreakoutConfiguration {Globals.Get<FreakoutConfiguration>()} - unfortunately, it's not possible to run two Freakout instances in the same process at the same time");
        }

        services.AddSingleton(configuration);

        services.AddHostedService(p =>
        {
            // HACK: Resolve the globals cleaner to make the container own it and therefore dispose it
            _ = p.GetRequiredService<GlobalsClearer>();

            var freakoutBackgroundService = new FreakoutBackgroundService(
                configuration: configuration,
                dispatcher: p.GetRequiredService<IOutboxCommandDispatcher>(),
                store: p.GetRequiredService<IOutboxCommandStore>(),
                logger: p.GetLoggerFor<FreakoutBackgroundService>()
            );
            return freakoutBackgroundService;
        });

        services.AddSingleton(configuration.CommandSerializer);

        services.AddSingleton<IOutboxCommandDispatcher, FreakoutOutboxCommandDispatcher>();

        // let the concrete configuration make its registrations
        configuration.InvokeConfigureServices(services);

        // must have IOutboxCommandStore implementation now
        if (!services.Any(s => s.ServiceType == typeof(IOutboxCommandStore)))
        {
            throw new ApplicationException($"The configuration {configuration} did not make the necessary IOutboxCommandStore registration");
        }

        // must have IOutbox implementation now
        if (!services.Any(s => s.ServiceType == typeof(IOutbox)))
        {
            throw new ApplicationException($"The configuration {configuration} did not make the necessary IOutbox registration");
        }

        services.TryAddSingleton<IFreakoutContextAccessor, AsyncLocalFreakoutContextAccessor>();

        // this is special and a monster hack: Stuff the configuration in the globals!
        Globals.Set(configuration);

        services.AddSingleton(_ => new GlobalsClearer());
    }

    /// <summary>
    /// Adds the type <typeparamref name="TCommandHandler"/> as a Freakout command handler.
    /// It is required that <typeparamref name="TCommandHandler"/> implements one or more <see cref="ICommandHandler{TCommand}"/> closed with a compatible type.
    /// </summary>
    public static void AddCommandHandler<TCommandHandler>(this IServiceCollection services) where TCommandHandler : class, ICommandHandler
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        var commandTypes = typeof(TCommandHandler)
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>))
            .Select(i => i.GetGenericArguments().First())
            .ToList();

        if (!commandTypes.Any())
        {
            throw new ArgumentException(
                $"The type {typeof(TCommandHandler)} cannot be registered as a command handler, because it doesn't implement ICommandHandler<TCommand>");
        }

        foreach (var commandType in commandTypes)
        {
            var serviceType = typeof(ICommandHandler<>).MakeGenericType(commandType);

            services.AddScoped(serviceType, typeof(TCommandHandler));
        }
    }
}