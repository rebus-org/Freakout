namespace Freakout;

public interface ICommandSerializer
{
    object Deserialize(OutboxCommand outboxCommand);
    SerializedCommand Serialize(object command);
}