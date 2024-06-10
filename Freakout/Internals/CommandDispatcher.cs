using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Freakout.Internals;

abstract class CommandDispatcher(ICommandSerializer commandSerializer, IServiceScopeFactory serviceScopeFactory) : ICommandDispatcher
{
    readonly ConcurrentDictionary<Type, Func<object, CancellationToken, Task>> _invokers = new();

    public async Task ExecuteAsync(OutboxCommand outboxCommand, CancellationToken cancellationToken = default)
    {
        var command = commandSerializer.Deserialize(outboxCommand);
        var type = command.GetType();

        var invoker = _invokers.GetOrAdd(type, CreateInvoker);

        await invoker(command, cancellationToken);
    }

    protected abstract Func<object, CancellationToken, Task> CreateInvoker(Type commandType);

    protected async Task ExecuteOutboxCommandGeneric<TCommand>(TCommand command, CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var handler = Resolve(scope.ServiceProvider);

        await handler.HandleAsync(command, cancellationToken);

        static ICommandHandler<TCommand> Resolve(IServiceProvider serviceProvider)
        {
            try
            {
                return serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Could not resolve ICommandHandler<{typeof(TCommand)}> from the container", exception);
            }
        }
    }

}