using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Freakout.Internals.Dispatch;

class FreakoutDispatcher(ICommandSerializer commandSerializer, IServiceScopeFactory serviceScopeFactory)
{
    readonly ConcurrentDictionary<Type, Func<object, CancellationToken, Task>> _invokers = new();

    public async Task ExecuteAsync(OutboxCommand outboxCommand, CancellationToken cancellationToken)
    {
        var command = commandSerializer.Deserialize(outboxCommand);
        var type = command.GetType();

        var invoker = _invokers.GetOrAdd(type, BuildInvoker);

        await invoker(command, cancellationToken);
    }

    Func<object, CancellationToken, Task> BuildInvoker(Type commandType)
    {
        const string methodName = nameof(ExecuteOutboxCommandGeneric);

        var genericExecuteMethod = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)?.MakeGenericMethod(commandType)
                                   ?? throw new ArgumentException($"Could not get generic method '{methodName}' closed with {commandType} for some reason");

        return (command, cancellationToken) => (Task)genericExecuteMethod.Invoke(this, [command, cancellationToken]);
    }

    async Task ExecuteOutboxCommandGeneric<TCommand>(TCommand command, CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<TCommand>>();

        await handler.HandleAsync(command, cancellationToken);
    }
}