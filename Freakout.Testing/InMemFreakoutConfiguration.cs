using System.Collections.Concurrent;
using Freakout.Testing.Internals;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable RedundantTypeArgumentsOfMethod

namespace Freakout.Testing;

/// <summary>
/// Freakout configuration for using an in-mem queue as the outbox. Useful in unit and integration testing scenarios
/// where it would be nice to be able to inspect enqueued outbox commands.
/// </summary>
public class InMemFreakoutConfiguration : FreakoutConfiguration
{
    /// <summary>
    /// Specifies whether serialization should be checked. Since this is pure in-mem messaging, it's entirely
    /// possible to bypass serializing/deserializing commands, but that would be less useful for testing, because
    /// serialization roundtripping errors would not be discovered.
    /// When TRUE, commands are roundtripped before being enqueued in the internal command queue.
    /// When FALSE, commands are just passed directly to the queue without any serialization.
    /// </summary>
    public bool CheckSerialization { get; set; } = true;

    /// <summary>
    /// Gets the in-mem command queue.
    /// </summary>
    public ConcurrentQueue<InMemOutboxCommand> Commands { get; } = new();

    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Commands);
        services.AddSingleton<IOutboxCommandStore, InMemOutboxCommandStore>();

        if (CheckSerialization)
        {
            services.AddSingleton<IOutbox>(_ => new InMemOutboxDecorator(CommandSerializer, new InMemOutbox(Commands)));
        }
        else
        {
            services.AddSingleton<IOutbox>(_ => new InMemOutbox(Commands));
        }
    }
}