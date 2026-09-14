using System;
using System.Threading;
using System.Threading.Tasks;
using Freakout.Tests.Contracts;

namespace Freakout.Tests;

public class GenericFreakoutContextWrapper(IFreakoutContext context, Func<CancellationToken, Task> completeAsync, Action dispose) : FreakoutContextWrapper
{
    public override IFreakoutContext Context => context;

    public override Task CompleteAsync(CancellationToken cancellationToken = default) => completeAsync(cancellationToken);

    public override void Dispose() => dispose();
}