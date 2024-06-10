using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Freakout.Internals.Dispatchers;

class IlEmitCommandDispatcher(ICommandSerializer commandSerializer, IServiceScopeFactory serviceScopeFactory) : CommandDispatcher(commandSerializer, serviceScopeFactory)
{
    /// <summary>
    /// This is how you build the same invoker using IL
    /// </summary>
    protected override Func<object, CancellationToken, Task> CreateInvoker(Type commandType)
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

        // load the command parameter (second argument) and convert it
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stloc_0); // store converted parameter into local
        il.Emit(OpCodes.Ldloc_0); // load converted parameter from local

        // load the CancellationToken parameter (third argument)
        il.Emit(OpCodes.Ldarg_2);

        // call the generic method
        il.Emit(OpCodes.Callvirt, genericMethod);

        // return the result of the call
        il.Emit(OpCodes.Ret);

        // create a delegate from the dynamic method
        var invoker = (Func<object, object, CancellationToken, Task>)dynamicMethod.CreateDelegate(
            typeof(Func<object, object, CancellationToken, Task>));

        // create a wrapper to match the required Func<object, CancellationToken, Task> signature
        return (obj, token) => invoker(this, obj, token);
    }
}