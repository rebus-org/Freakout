using System;
using Freakout.Internals;

namespace Freakout.Tests;

class GlobalsCleaner : IDisposable
{
    public void Dispose() => Globals.Clear();
}