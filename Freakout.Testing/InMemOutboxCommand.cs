using System.Collections.Generic;

namespace Freakout.Testing;

/// <summary>
/// Holds the information of an in-mem outbox command.
/// </summary>
public record InMemOutboxCommand(Dictionary<string, string> Headers, object Command);