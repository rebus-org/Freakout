using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Freakout.Internals.Dispatchers;

class CompiledExpressionCommandDispatcher(ICommandSerializer commandSerializer, IServiceScopeFactory serviceScopeFactory) : CommandDispatcher(commandSerializer, serviceScopeFactory)
{
    /// <summary>
    /// This is how you build an invoker for a generic method using expression trees
    /// </summary>
    protected override Func<object, CancellationToken, Task> CreateInvoker(Type commandType)
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