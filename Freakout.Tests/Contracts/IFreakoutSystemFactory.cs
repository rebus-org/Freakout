using System;
using System.Threading.Tasks;
using Freakout.Internals;
using Microsoft.Extensions.DependencyInjection;

namespace Freakout.Tests.Contracts;

public interface IFreakoutSystemFactory : IDisposable
 {
    Task<FreakoutSystem> CreateAsync();
}

public class FreakoutSystem(IServiceProvider ServiceProvider)
{
    /// <summary>
    /// HACK: Resolve this one, so the container owns it and will clean the globals on disposal
    /// </summary>
    readonly object _ = ServiceProvider.GetRequiredService<GlobalsClearer>();

    public IOutbox Outbox => ServiceProvider.GetRequiredService<IOutbox>();
    
    public IOutboxCommandStore OutboxCommandStore => ServiceProvider.GetRequiredService<IOutboxCommandStore>();
}