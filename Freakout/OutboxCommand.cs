using System.Collections.Generic;

namespace Freakout;

/// <summary>
/// Raw outbox command
/// </summary>
public record OutboxCommand(Dictionary<string, string> Headers, byte[] Payload);