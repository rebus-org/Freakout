using System;
using System.Collections.Generic;

namespace Freakout;

/// <summary>
/// Raw store command after having been persisted
/// </summary>
public record PersistentOutboxCommand(Guid Id, DateTimeOffset Created, Dictionary<string, string> Headers, byte[] Payload) : OutboxCommand(Headers, Payload);