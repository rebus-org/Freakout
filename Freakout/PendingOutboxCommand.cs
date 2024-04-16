using System;
using System.Collections.Generic;

namespace Freakout;

/// <summary>
/// Raw store command after having been persisted
/// </summary>
public record PendingOutboxCommand(Guid Id, DateTimeOffset Created, Dictionary<string, string> Headers, byte[] Payload)
    : OutboxCommand(Headers, Payload)
{
    /// <summary>
    /// Gets the command state
    /// </summary>
    public CommandState State { get; private set; } = new PendingCommandState();

    /// <summary>
    /// Sets <see cref="State"/> to <paramref name="state"/>
    /// </summary>
    public void SetState(CommandState state) => State = state ?? throw new ArgumentNullException(nameof(state));
}

/// <summary>
/// Abstract command state. Represents a state that a command can be in.
/// </summary>
public abstract record CommandState;

/// <summary>
/// Represents the state of the command when it has been fetched from the command store.
/// </summary>
public record PendingCommandState : CommandState;

/// <summary>
/// Represents the state of the command when it has been successfully executed and executing it took <see cref="Elapsed"/>
/// </summary>
public record SuccessfullyExecutedCommandState(TimeSpan Elapsed) : CommandState;

/// <summary>
/// Represents the state of the command when executing it failed with <see cref="Exception"/> and took <see cref="Elapsed"/>
/// </summary>
public record FailedCommandState(TimeSpan Elapsed, Exception Exception) : CommandState;