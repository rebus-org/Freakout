﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Freakout;

public interface IOutbox
{
    Task<OutboxCommandBatch> GetPendingOutboxCommandsAsync(CancellationToken cancellationToken = default);
}

public class OutboxCommandBatch(IEnumerable<OutboxCommand> outboxCommands, Func<CancellationToken, Task> completeAsync) : IEnumerable<OutboxCommand>
{
    public Task CompleteAsync(CancellationToken cancellationToken = default) => completeAsync(cancellationToken);

    public IEnumerator<OutboxCommand> GetEnumerator() => outboxCommands.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public record OutboxCommand(DateTimeOffset Time, Dictionary<string, string> Headers, byte[] Payload);

