namespace Freakout;

/// <summary>
/// Keys of headers that have special meaning in Freakout. Please use only if you know what you're doing ;)
/// </summary>
public static class HeaderKeys
{
    /// <summary>
    /// The ID that the command has in the data store
    /// </summary>
    public const string CommandId = "cmd-id";

    /// <summary>
    /// Type information for the (de)serializer to use to be able to construct a command object
    /// </summary>
    public const string CommandType = "cmd-type";

    /// <summary>
    /// MIME type of the serialized payload
    /// </summary>
    public const string ContentType = "content-type";
}