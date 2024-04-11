using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Freakout;

public class OutboxCommandBatch(IEnumerable<OutboxCommand> outboxCommands, Func<CancellationToken, Task> completeAsync) : IEnumerable<OutboxCommand>
{
    public static readonly OutboxCommandBatch Empty = new(Array.Empty<OutboxCommand>(), _ => Task.CompletedTask);

    public Task CompleteAsync(CancellationToken cancellationToken = default) => completeAsync(cancellationToken);

    public IEnumerator<OutboxCommand> GetEnumerator() => outboxCommands.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}