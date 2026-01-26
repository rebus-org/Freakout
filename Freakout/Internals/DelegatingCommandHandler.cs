using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Freakout.Internals;

/// <summary>
/// Built-in generic command handler that just delegates its invocation to the given <paramref name="invoker"/> function.
/// </summary>
class DelegatingCommandHandler<TCommand>(Func<TCommand, IDictionary<string, string>, CancellationToken, Task> invoker) : ICommandHandler<TCommand>
{
    public Task HandleAsync(TCommand command, IDictionary<string, string> headers, CancellationToken cancellationToken) => invoker(command, headers, cancellationToken);
}