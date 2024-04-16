using System.Collections.Generic;

namespace Freakout;

/// <summary>
/// Raw store command before being persisted
/// </summary>
public record OutboxCommand(Dictionary<string, string> Headers, byte[] Payload);