using System;
using System.Collections.Generic;

namespace Freakout;

/// <summary>
/// Raw store command before being persisted
/// </summary>
public record OutboxCommand(Dictionary<string, string> Headers, byte[] Payload);

/// <summary>
/// Raw store command after having been persisted
/// </summary>
public record PersistentOutboxCommand(Guid Id, DateTimeOffset Created, Dictionary<string, string> Headers, byte[] Payload) : OutboxCommand(Headers, Payload);