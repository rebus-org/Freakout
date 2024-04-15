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
public class OutboxCommandBatch(IFreakoutContext freakoutContext, IEnumerable<OutboxCommand> outboxCommands, Func<CancellationToken, Task> completeAsync, Action dispose) : IEnumerable<OutboxCommand>, IDisposable
{
    /// <summary>
    /// Gets an empty <see cref="OutboxCommandBatch"/>
    /// </summary>
    public static readonly OutboxCommandBatch Empty = new(null, Array.Empty<OutboxCommand>(), _ => Task.CompletedTask, () => { });

    internal Task CompleteAsync(CancellationToken cancellationToken = default) => completeAsync(cancellationToken);

    /// <summary>
    /// Gets the Freakout context that wraps the transaction that this command batch does its work in
    /// </summary>
    public IFreakoutContext FreakoutContext => freakoutContext;

    /// <summary>
    /// Gets an enumerator for the contained store commands
    /// </summary>
    public IEnumerator<OutboxCommand> GetEnumerator() => outboxCommands.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Disposes and cleans up whatever resources need to be cleaned up
    /// </summary>
    public void Dispose() => dispose();
}