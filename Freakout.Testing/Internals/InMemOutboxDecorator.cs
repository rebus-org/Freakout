using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Freakout.Testing.Internals;

class InMemOutboxDecorator(ICommandSerializer serializer, IOutbox outbox) : IOutbox
{
    public void AddOutboxCommand(object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
    {
        CheckSerialization(command);
        outbox.AddOutboxCommand(command, headers, cancellationToken);
    }

    public async Task AddOutboxCommandAsync(object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
    {
        CheckSerialization(command);
        await outbox.AddOutboxCommandAsync(command, headers, cancellationToken);
    }

    void CheckSerialization(object command)
    {
        try
        {
            var outboxCommand = serializer.Serialize(command);
            var roundtrippedCommand = serializer.Deserialize(outboxCommand);
        }
        catch (Exception exception)
        {
            throw new SerializationException(
                $"Serialization check failed – the command {command} could not be roundtripped by serializer {serializer.GetType().Name}", exception);
        }
    }
}