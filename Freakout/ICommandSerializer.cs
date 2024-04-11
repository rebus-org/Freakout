namespace Freakout;

public interface ICommandSerializer
{
    object Deserialize(OutboxCommand outboxCommand);
}