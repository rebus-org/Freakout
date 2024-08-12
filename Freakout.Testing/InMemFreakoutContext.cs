using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Freakout.Testing;

/// <summary>
/// Implementation of <see cref="IFreakoutContext"/> that buffers messages in memory
/// </summary>
public class InMemFreakoutContext : IContextHooks
{
    readonly ConcurrentQueue<InMemOutboxCommand> _commands = new();

    internal void Enlist(InMemOutboxCommand command) => _commands.Enqueue(command);

    internal Action<IEnumerable<InMemOutboxCommand>> UnmountedCallback;

    /// <inheritdoc />
    public void Mounted()
    {
    }

    /// <inheritdoc />
    public void Unmounted() => UnmountedCallback?.Invoke(_commands);
}