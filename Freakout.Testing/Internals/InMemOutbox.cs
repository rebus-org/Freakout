using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Freakout.Testing.Internals;

class InMemOutbox(ConcurrentQueue<InMemOutboxCommand> outboxCommands) : IOutbox
{
    public event Action<InMemOutboxCommand> CommandAddedToQueue;

    public void AddOutboxCommand(object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
    {
        Enqueue(command, headers);
    }

    public async Task AddOutboxCommandAsync(object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
    {
        Enqueue(command, headers);
    }

    void Enqueue(object command, Dictionary<string, string> headers)
    {
        var inMemOutboxCommand = new InMemOutboxCommand(headers ?? new(), command);
        outboxCommands.Enqueue(inMemOutboxCommand);
        CommandAddedToQueue?.Invoke(inMemOutboxCommand);
    }
}