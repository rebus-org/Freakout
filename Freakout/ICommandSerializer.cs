namespace Freakout;

/// <summary>
/// Command serializer. Must be capable of serializing all the relevant commands
/// </summary>
public interface ICommandSerializer
{
    /// <summary>
    /// Serializes the given <paramref name="command"/> into an <see cref="OutboxCommand"/>.
    /// Please note that the headers returned in the <see cref="OutboxCommand"/> are highly likely
    /// to be important to the deserialization process, so please only tamper with them if you know what you're doing.
    /// </summary>
    OutboxCommand Serialize(object command);

    /// <summary>
    /// Deserializes the given <paramref name="outboxCommand"/> into a copy of the original command.
    /// </summary>
    object Deserialize(OutboxCommand outboxCommand);
}