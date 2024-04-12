using System;
using System.Collections.Generic;

namespace Freakout.MsSql;

record MsSqlOutboxCommand(Guid Id, DateTimeOffset Time, Dictionary<string, string> Headers, byte[] Payload) : OutboxCommand(Headers, Payload);
