using System.Threading;
// ReSharper disable StaticMemberInGenericType
// ReSharper disable ClassNeverInstantiated.Local

namespace Freakout.Internals;

public static class Globals
{
    static int IndexCounter;

    static readonly object[] Items = new object[256];

    public static T Get<T>() => (T)Items[GlobalIndexer<T>.Index];

    public static void Set<T>(T t) => Items[GlobalIndexer<T>.Index] = t;

    class GlobalIndexer<T>
    {
        internal static readonly int Index = Interlocked.Increment(ref IndexCounter);
    }

    internal static void Clear()
    {
        for (var index = 0; index < Items.Length; index++)
        {
            Items[index] = null;
        }
    }
}