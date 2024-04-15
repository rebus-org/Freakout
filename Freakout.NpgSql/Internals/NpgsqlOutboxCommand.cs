using System;
using System.Collections.Generic;

namespace Freakout.NpgSql.Internals;

record NpgsqlOutboxCommand(Guid Id, DateTimeOffset Time, Dictionary<string, string> Headers, byte[] Payload) : OutboxCommand(Headers, Payload);