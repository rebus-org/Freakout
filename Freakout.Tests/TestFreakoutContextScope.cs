using System.Threading.Tasks;
using Freakout.Internals;
using NUnit.Framework;
using Testy;

namespace Freakout.Tests;

[TestFixture]
public class TestFreakoutContextScope : FixtureBase
{
    [Test]
    public void ItWorks_SameThreadSameContext()
    {
        var context1 = new MyLittleContextThing();
        var context2 = new MyLittleContextThing();

        using (new FreakoutContextScope(context1))
        {
            var ambientContext = new AsyncLocalFreakoutContextAccessor().GetContext<MyLittleContextThing>();

            Assert.That(ambientContext, Is.SameAs(context1));
        }

        using (new FreakoutContextScope(context2))
        {
            var ambientContext = new AsyncLocalFreakoutContextAccessor().GetContext<MyLittleContextThing>();

            Assert.That(ambientContext, Is.SameAs(context2));
        }
    }

    [Test]
    public async Task ItWorks_ParallelAction()
    {
        var context1 = new MyLittleContextThing();
        var context2 = new MyLittleContextThing();

        async Task<bool> CheckTheContext(MyLittleContextThing context)
        {
            using (new FreakoutContextScope(context))
            {
                await Task.Delay(millisecondsDelay: 200);

                var ambientContext = new AsyncLocalFreakoutContextAccessor().GetContext<MyLittleContextThing>();

                return ambientContext == context;
            }
        }

        var t1 = CheckTheContext(context1);
        var t2 = CheckTheContext(context2);

        var result1 = await t1;
        var result2 = await t2;

        Assert.That(result1, Is.True);
        Assert.That(result2, Is.True);
    }

    record MyLittleContextThing : IFreakoutContext;
}