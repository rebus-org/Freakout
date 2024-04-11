using System.Collections.Generic;
using System.Text.Json;

namespace Freakout.Serialization;

public static class HeaderSerializer
{
    public static string SerializeToString(Dictionary<string, string> headers) => JsonSerializer.Serialize(headers);

    public static Dictionary<string, string> DeserializeFromString(string headers) => JsonSerializer.Deserialize<Dictionary<string, string>>(headers);
}