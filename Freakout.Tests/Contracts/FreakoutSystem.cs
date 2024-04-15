using System;
using Freakout.Internals;
using Microsoft.Extensions.DependencyInjection;

namespace Freakout.Tests.Contracts;

public class FreakoutSystem(IServiceProvider ServiceProvider, Func<IFreakoutContext> contextFactory, Action<IFreakoutContext> commitAction, Action<IFreakoutContext> disposeAction)
{
    /// <summary>
    /// HACK: Resolve this one, so the container owns it and will clean the globals on disposal
    /// </summary>
    readonly object _ = ServiceProvider.GetRequiredService<GlobalsClearer>();

    public IOutbox Outbox => ServiceProvider.GetRequiredService<IOutbox>();

    public IOutboxCommandStore OutboxCommandStore => ServiceProvider.GetRequiredService<IOutboxCommandStore>();

    public FreakoutTestScope CreateScope() => new(contextFactory(), commitAction, disposeAction);

    public class FreakoutTestScope(IFreakoutContext freakoutContext, Action<IFreakoutContext> commitAction, Action<IFreakoutContext> disposeAction) : IDisposable
    {
        readonly FreakoutContextScope _innerScope = new(freakoutContext);

        public void Complete() => commitAction(freakoutContext);

        public void Dispose()
        {
            _innerScope.Dispose();
            disposeAction(freakoutContext);
        }
    }
}