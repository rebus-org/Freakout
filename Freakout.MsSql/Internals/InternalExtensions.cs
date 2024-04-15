using System;
using System.Collections.Generic;

namespace Freakout.MsSql.Internals;

static class InternalExtensions
{
    public static void InsertInto(this Dictionary<string, string> source, Dictionary<string,string> target)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (target == null) throw new ArgumentNullException(nameof(target));

        foreach (var kvp in source)
        {
            target[kvp.Key] = kvp.Value;
        }
    }
}