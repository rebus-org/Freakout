using System;
using System.Collections.Generic;

namespace Freakout.MsSql;

public record MsSqlOutboxCommand(Guid Id, DateTimeOffset Time, Dictionary<string, string> Headers, byte[] Payload) : OutboxCommand(Time, Headers, Payload);
