using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Freakout.Internals;

namespace Freakout;

/// <summary>
/// Represents a batch of store commands to be processed. When completed without errors, Freakout will call its completion method, which
/// should cause the store to mark the contained commands as handled.
/// </summary>
public class OutboxCommandBatch(IFreakoutContext freakoutContext, IEnumerable<PendingOutboxCommand> outboxCommands, Func<CancellationToken, Task> completeAsync, Action dispose) : IEnumerable<PendingOutboxCommand>, IDisposable
{
    /// <summary>
    /// Gets an empty <see cref="OutboxCommandBatch"/>
    /// </summary>
    public static readonly OutboxCommandBatch Empty = new(new EmptyFreakoutContext(), Array.Empty<PendingOutboxCommand>(), _ => Task.CompletedTask, () => { });

    internal Task CompleteAsync(CancellationToken cancellationToken = default) => completeAsync(cancellationToken);

    /// <summary>
    /// Gets the Freakout context that wraps the transaction that this command batch does its work in
    /// </summary>
    public IFreakoutContext FreakoutContext => freakoutContext;

    /// <summary>
    /// Gets an enumerator for the contained store commands
    /// </summary>
    public IEnumerator<PendingOutboxCommand> GetEnumerator() => outboxCommands.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Disposes and cleans up whatever resources need to be cleaned up
    /// </summary>
    public void Dispose() => dispose();
}