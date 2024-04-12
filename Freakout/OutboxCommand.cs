using System.Collections.Generic;

namespace Freakout;

/// <summary>
/// Raw store command
/// </summary>
public record OutboxCommand(Dictionary<string, string> Headers, byte[] Payload);