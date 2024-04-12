using System.Collections.Generic;
using System.Text.Json;

namespace Freakout.Serialization;

/// <summary>
/// Built-in header serializer. This is just how store command headers are serialized. Uses System.Text.Json internally.
/// </summary>
public static class HeaderSerializer
{
    /// <summary>
    /// Serializes the <paramref name="headers"/> to a string.
    /// </summary>
    public static string SerializeToString(Dictionary<string, string> headers) => JsonSerializer.Serialize(headers);

    /// <summary>
    /// Deserializes the <paramref name="headers"/> string back to a Dictionary&lt;string, string&gt;
    /// </summary>
    public static Dictionary<string, string> DeserializeFromString(string headers) => JsonSerializer.Deserialize<Dictionary<string, string>>(headers);
}