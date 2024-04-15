using System;
using System.Threading;

namespace Freakout.Internals;

class AsyncLocalFreakoutContextAccessor : IFreakoutContextAccessor
{
    internal static readonly AsyncLocal<IFreakoutContext> Instance = new();

    public TContext GetContext<TContext>(bool throwIfNull = true) where TContext : class, IFreakoutContext
    {
        var instance = Instance.Value;

        if (instance == null)
        {
            if (!throwIfNull) return null;

            throw new InvalidOperationException("Could not get ambient Frekout context. Please be sure that a suitable ambient context is available by using FreakoutContextScope");
        }

        if (instance is TContext context) return context;

        throw new InvalidCastException(
            $"Ambient Freakout context of type {instance.GetType()} cannot be cast to {typeof(TContext)}");
    }
}