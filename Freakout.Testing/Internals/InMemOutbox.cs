using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Freakout.Testing.Internals;

class InMemOutbox(ConcurrentQueue<InMemOutboxCommand> outboxCommands) : IOutbox
{
    public void AddOutboxCommand(object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
    {
        outboxCommands.Enqueue(new(headers ?? new(), command));
    }

    public async Task AddOutboxCommandAsync(object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
    {
        outboxCommands.Enqueue(new(headers ?? new(), command));
    }
}