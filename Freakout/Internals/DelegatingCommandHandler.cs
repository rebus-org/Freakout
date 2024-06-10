using System;
using System.Threading;
using System.Threading.Tasks;

namespace Freakout.Internals;

/// <summary>
/// Built-in generic command handler that just delegates its invocation to the given <paramref name="invoker"/> function.
/// </summary>
class DelegatingCommandHandler<TCommand>(Func<TCommand, CancellationToken, Task> invoker) : ICommandHandler<TCommand>
{
    public Task HandleAsync(TCommand command, CancellationToken cancellationToken) => invoker(command, cancellationToken);
}