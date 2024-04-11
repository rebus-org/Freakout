namespace Freakout;

public record struct SerializedCommand(string TypeHeader, byte[] Payload);