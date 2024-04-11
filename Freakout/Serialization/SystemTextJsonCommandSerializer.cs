using System;
using System.Collections.Generic;
using System.Text.Json;
using Freakout.Internals;

namespace Freakout.Serialization;

public class SystemTextJsonCommandSerializer : ICommandSerializer
{
    public SerializedCommand Serialize(object command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var type = command.GetType().GetSimpleAssemblyQualifiedName();
        var payload = JsonSerializer.SerializeToUtf8Bytes(command);

        return new(TypeHeader: type, Payload: payload);
    }

    public object Deserialize(OutboxCommand outboxCommand)
    {
        if (outboxCommand == null) throw new ArgumentNullException(nameof(outboxCommand));

        var typeHeader = outboxCommand.Headers.TryGetValue(HeaderKeys.Type, out var result)
            ? result
            : throw new KeyNotFoundException($"Could not find header with key '{HeaderKeys.Type}'");

        var type = Type.GetType(typeHeader) ?? throw new ArgumentException($"Could not load type '{typeHeader}'");

        var commandObject = JsonSerializer.Deserialize(outboxCommand.Payload, type);

        return commandObject;
    }
}