using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Freakout.Tests.Contracts;

public class FreakoutSystem(ServiceProvider serviceProvider, Func<IFreakoutContext> contextFactory, Action<IFreakoutContext> commitAction, Action<IFreakoutContext> disposeAction)
{
    public IOutbox Outbox => serviceProvider.GetRequiredService<IOutbox>();

    public IOutboxCommandStore OutboxCommandStore => serviceProvider.GetRequiredService<IOutboxCommandStore>();
    
    public ICommandSerializer CommandSerializer => serviceProvider.GetRequiredService<ICommandSerializer>();

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

    public Task StartCommandProcessorAsync(CancellationToken stoppingToken)
    {
        return serviceProvider.RunBackgroundWorkersAsync(stoppingToken);
    }
}