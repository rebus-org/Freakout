using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
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

        var invoker = _invokers.GetOrAdd(type, CreateInvokerWithCompiledExpression);

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
    Func<object, CancellationToken, Task> CreateInvokerWithCompiledExpression(Type commandType)
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

    /// <summary>
    /// This is how you build the same invoker using IL
    /// </summary>
    Func<object, CancellationToken, Task> CreateInvokerWithIl(Type commandType)
    {
        const string methodName = nameof(ExecuteOutboxCommandGeneric);

        // get method to call
        var type = GetType();
        var methodInfo = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                         ?? throw new ArgumentException($"Could not get non-public instance method '{methodName}' from {type}");

        // close the generic method
        var genericMethod = methodInfo.MakeGenericMethod(commandType);

        // create dynamic method
        var dynamicMethod = new DynamicMethod(
            name: "DynamicInvoker",
            returnType: typeof(Task),
            parameterTypes: [typeof(object), typeof(object), typeof(CancellationToken)],
            m: type.Module
        );

        // get IL generator
        var il = dynamicMethod.GetILGenerator();

        // declare a local to store the converted command parameter
        il.DeclareLocal(commandType);

        // load "this" (first argument) onto the evaluation stack
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Castclass, type);

        // load the command parameter (second argument) and convert it
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Castclass, commandType);
        il.Emit(OpCodes.Stloc_0); // store converted parameter into local
        il.Emit(OpCodes.Ldloc_0); // load converted parameter from local

        // load the CancellationToken parameter (third argument)
        il.Emit(OpCodes.Ldarg_2);

        // call the generic method
        il.Emit(OpCodes.Call, genericMethod);

        // return the result of the call
        il.Emit(OpCodes.Ret);

        // create a delegate from the dynamic method
        var invoker = (Func<object, object, CancellationToken, Task>)dynamicMethod.CreateDelegate(
            typeof(Func<object, object, CancellationToken, Task>));

        // create a wrapper to match the required Func<object, CancellationToken, Task> signature
        return (obj, token) => invoker(this, obj, token);
    }
}