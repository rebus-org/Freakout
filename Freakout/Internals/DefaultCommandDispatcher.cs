using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Freakout.Internals;

class DefaultCommandDispatcher(ICommandSerializer commandSerializer, IServiceScopeFactory serviceScopeFactory) : ICommandDispatcher
{
    readonly ConcurrentDictionary<Type, Func<object, CancellationToken, Task>> _invokers = new();

    public async Task ExecuteAsync(OutboxCommand outboxCommand, CancellationToken cancellationToken = default)
    {
        var command = commandSerializer.Deserialize(outboxCommand);
        var type = command.GetType();

        var invoker = _invokers.GetOrAdd(type, CreateInvoker);

        await invoker(command, cancellationToken);
    }

    async Task ExecuteOutboxCommandGeneric<TCommand>(TCommand command, CancellationToken cancellationToken)
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

    /// <summary>
    /// This is how you build an invoker for a generic method using expression trees
    /// </summary>
    Func<object, CancellationToken, Task> CreateInvoker(Type commandType)
    {
        const string methodName = nameof(ExecuteOutboxCommandGeneric);

        var type = GetType();
        
        // get method to call
        var methodInfo = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                         ?? throw new ArgumentException($"Could not get non-public instance method '{methodName}' from {type}");

        // close the generic method
        var genericMethod = methodInfo.MakeGenericMethod(commandType);

        // get reference to this
        var instance = Expression.Constant(this);

        // get parameters
        var commandParameter = Expression.Parameter(typeof(object), "command");
        var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        // and convert the System.Object input to commandType
        var commandConversion = Expression.Convert(commandParameter, commandType);

        // build the call
        var call = Expression.Call(instance, genericMethod, commandConversion, cancellationTokenParameter);

        // and wrap it in a lambda with a signature we can use
        var lambda = Expression.Lambda<Func<object, CancellationToken, Task>>(call, commandParameter, cancellationTokenParameter);

        return lambda.Compile();
    }
}