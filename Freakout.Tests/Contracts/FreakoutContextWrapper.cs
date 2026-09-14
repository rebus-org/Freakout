using System;
using System.Threading;
using System.Threading.Tasks;

namespace Freakout.Tests.Contracts;

public abstract class FreakoutContextWrapper : IDisposable
{
    /// <summary>
    /// Gets the wrapped <see cref="IFreakoutContext"/>
    /// </summary>
    public abstract IFreakoutContext Context { get; }

    public abstract Task CompleteAsync(CancellationToken cancellationToken = default);
    
    public abstract void Dispose();
}