using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Freakout;

/// <summary>
/// Represents a batch of store commands to be processed. When completed without errors, Freakout will call its completion method, which
/// should cause the store to mark the contained commands as handled.
/// </summary>
public class OutboxCommandBatch(IEnumerable<OutboxCommand> outboxCommands, Func<CancellationToken, Task> completeAsync) : IEnumerable<OutboxCommand>
{
    /// <summary>
    /// Gets an empty <see cref="OutboxCommandBatch"/>
    /// </summary>
    public static readonly OutboxCommandBatch Empty = new(Array.Empty<OutboxCommand>(), _ => Task.CompletedTask);

    internal Task CompleteAsync(CancellationToken cancellationToken = default) => completeAsync(cancellationToken);

    /// <summary>
    /// Gets an enumerator for the contained store commands
    /// </summary>
    public IEnumerator<OutboxCommand> GetEnumerator() => outboxCommands.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}