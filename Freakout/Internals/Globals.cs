using System.Threading;
// ReSharper disable StaticMemberInGenericType
// ReSharper disable ClassNeverInstantiated.Local

namespace Freakout.Internals;

/// <summary>
/// Provides a central place for type-based registrations of things.
/// </summary>
public static class Globals
{
    static int IndexCounter;

    static readonly object[] Items = new object[256];

    /// <summary>
    /// Gets the global object of type <typeparamref name="T"/>
    /// </summary>
    public static T Get<T>() => (T)Items[GlobalIndexer<T>.Index];

    /// <summary>
    /// Sets the global object of type <typeparamref name="T"/>
    /// </summary>
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