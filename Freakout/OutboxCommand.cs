using System;
using System.Collections.Generic;

namespace Freakout;

public record OutboxCommand(DateTimeOffset Time, Dictionary<string, string> Headers, byte[] Payload);