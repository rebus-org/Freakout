using System;
using Freakout.Internals;

namespace Freakout.Tests;

public class GlobalsCleaner : IDisposable
{
    public void Dispose() => Globals.Clear();
}