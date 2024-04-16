using System;
using Freakout.Config;
using Freakout.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Freakout;

/// <summary>
/// Base configuration class. Use a derived configuration class (e.g. "MsSqlFreakoutConfiguration" from the "Freakout.MsSql" NuGet package) to
/// pass to <see cref="FreakoutServiceCollectionExtensions.AddFreakout"/>.
/// </summary>
public abstract class FreakoutConfiguration
{
    internal void InvokeConfigureServices(IServiceCollection services) => ConfigureServices(services);

    /// <summary>
    /// Must carry out the necessary registrations in <paramref name="services"/> to be able to work.
    /// At a minimum, this includes registrations for
    /// <list type="bullet">
    /// <item><see cref="IOutbox"/></item>
    /// <item><see cref="IOutboxCommandStore"/></item>
    /// </list>
    /// and then whatever stuff the selected type of persistence needs to do its thing.
    /// </summary>
    protected abstract void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Configures the poll interval, i.e. how long to wait between polling the store for pending commands.
    /// </summary>
    public TimeSpan OutboxPollInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Configures the command processing batch size, i.e. how many outbox commands to fetch, execute, and complete per batch.
    /// </summary>
    public int CommandProcessingBatchSize { get; set; } = 1;

    /// <summary>
    /// Configures the command serializer. Defaults to <see cref="SystemTextJsonCommandSerializer"/> which uses System.Text.Json to serialize commands.
    /// </summary>
    public ICommandSerializer CommandSerializer { get; set; } = new SystemTextJsonCommandSerializer();
}