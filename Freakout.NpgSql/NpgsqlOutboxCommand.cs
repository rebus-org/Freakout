using System;
using System.Collections.Generic;

namespace Freakout.NpgSql;

record NpgsqlOutboxCommand(Guid Id, DateTimeOffset Time, Dictionary<string, string> Headers, byte[] Payload) : OutboxCommand(Headers, Payload);