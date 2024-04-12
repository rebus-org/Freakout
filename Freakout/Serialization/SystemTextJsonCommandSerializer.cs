using System;
using System.Collections.Generic;
using System.Text.Json;
using Freakout.Internals;

namespace Freakout.Serialization;

/// <summary>
/// Implementation of <see cref="ICommandSerializer"/> that uses System.Text.Json to do its thing.
/// </summary>
public class SystemTextJsonCommandSerializer : ICommandSerializer
{
    const string JsonContentType = "application/json; encoding=utf-8";

    /// <summary>
    /// Serializes the command as UTF8-encoded JSON and inserts the "short, assembly-qualified type name" as
    /// the <see cref="HeaderKeys.CommandType"/> header. Moreover, the <see cref="HeaderKeys.ContentType"/> header
    /// will be set to 'application/json; encoding=utf-8'.
    /// </summary>
    public OutboxCommand Serialize(object command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var type = command.GetType().GetSimpleAssemblyQualifiedName();
        var payload = JsonSerializer.SerializeToUtf8Bytes(command);
        var headers = new Dictionary<string, string>
        {
            [HeaderKeys.CommandType] = type,
            [HeaderKeys.ContentType] = JsonContentType,
        };

        return new(headers, Payload: payload);
    }

    /// <summary>
    /// Deserializes the <paramref name="outboxCommand"/>, assuming it contains JSON. Will check that the
    /// <see cref="HeaderKeys.ContentType"/> header has the value 'application/json; encoding=utf-8' and
    /// will determine which type to deserialize as by loading the .NET type specified by the
    /// <see cref="HeaderKeys.CommandType"/> header.
    /// </summary>
    public object Deserialize(OutboxCommand outboxCommand)
    {
        if (outboxCommand == null) throw new ArgumentNullException(nameof(outboxCommand));

        var contentTypeHeader = outboxCommand.Headers.GetValueOrThrow(HeaderKeys.ContentType);

        if (!string.Equals(contentTypeHeader, JsonContentType))
        {
            throw new FormatException($"The '{HeaderKeys.ContentType}' was '{contentTypeHeader}' and not '{JsonContentType}' as expected");
        }

        var typeHeader = outboxCommand.Headers.GetValueOrThrow(HeaderKeys.CommandType);

        var type = LoadType();

        var commandObject = JsonSerializer.Deserialize(outboxCommand.Payload, type);

        return commandObject;

        Type LoadType()
        {
            try
            {
                return Type.GetType(typeHeader) ?? throw new ArgumentException("Type was not found in the current app domain");
            }
            catch (Exception exception)
            {
                throw new ArgumentException($"Could not load type '{typeHeader}'", exception);
            }
        }
    }
}