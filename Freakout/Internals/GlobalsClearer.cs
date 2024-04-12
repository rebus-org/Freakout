using System;

namespace Freakout.Internals;

class GlobalsClearer : IDisposable
{
    public void Dispose() => Globals.Clear();
}