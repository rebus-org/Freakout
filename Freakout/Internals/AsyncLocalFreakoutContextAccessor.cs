using System;
using System.Threading;

namespace Freakout.Internals;

class AsyncLocalFreakoutContextAccessor : IFreakoutContextAccessor
{
    internal static readonly AsyncLocal<IFreakoutContext> Instance = new();

    public TContext GetContext<TContext>() where TContext : class, IFreakoutContext
    {
        var instance = Instance.Value;
        if (instance == null) return null;

        if (instance is TContext context) return context;

        throw new InvalidCastException(
            $"Ambient Freakout context of type {instance.GetType()} cannot be cast to {typeof(TContext)}");
    }
}