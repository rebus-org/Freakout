using Freakout.Internals;
using NUnit.Framework;

namespace Freakout.Tests;

[TestFixture]
public class TestGlobals
{
    [Test]
    public void CanStashSomeStuff()
    {
        var stuff = new Stuff1();

        Globals.Set(stuff);

        var roundtrippedStuff = Globals.Get<Stuff1>();

        Assert.That(roundtrippedStuff, Is.SameAs(stuff));
    }
    
    [Test]
    public void CanStashSomeStuff_TwoStuffs()
    {
        var stuff1 = new Stuff1();
        var stuff2 = new Stuff2();

        Globals.Set(stuff1);
        Globals.Set(stuff2);

        var roundtrippedStuff1 = Globals.Get<Stuff1>();
        var roundtrippedStuff2 = Globals.Get<Stuff2>();

        Assert.That(roundtrippedStuff1, Is.SameAs(stuff1));
        Assert.That(roundtrippedStuff2, Is.SameAs(stuff2));
    }

    [Test]
    public void CanStashSomeStuff_NotSameReference()
    {
        var stuff11 = new Stuff1();
        var stuff12 = new Stuff1();

        Globals.Set(stuff11);
        Globals.Clear();
        Globals.Set(stuff12);

        var roundtrippedStuff = Globals.Get<Stuff1>();

        Assert.That(roundtrippedStuff, Is.SameAs(stuff12));
    }

    record Stuff1;
    record Stuff2;
}