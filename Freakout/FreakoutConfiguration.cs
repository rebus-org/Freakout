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
    internal abstract void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Configures the poll interval, i.e. how long to wait between polling the store for pending commands.
    /// </summary>
    public TimeSpan OutboxPollInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Configures the command serializer. Defaults to <see cref="SystemTextJsonCommandSerializer"/> which uses System.Text.Json to serialize commands.
    /// </summary>
    public ICommandSerializer CommandSerializer { get; set; } = new SystemTextJsonCommandSerializer();
}