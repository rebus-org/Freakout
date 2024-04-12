namespace Freakout;

/// <summary>
/// Keys of headers that have special meaning in Freakout. Please use only if you know what you're doing ;)
/// </summary>
public static class HeaderKeys
{
    /// <summary>
    /// Type information for the (de)serializer to use to be able to construct a command object
    /// </summary>
    public const string CommandType = "cmd-type";

    /// <summary>
    /// MIME type of the serialized payload
    /// </summary>
    public const string ContentType = "content-type";
}