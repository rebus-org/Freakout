using System.Linq;
using System.Threading.Tasks;
using Freakout.Internals;
using NUnit.Framework;
using Testy;

namespace Freakout.Tests.Contracts;

public abstract class NormalTests<TFreakoutSystemFactory> : FixtureBase where TFreakoutSystemFactory : IFreakoutSystemFactory, new()
{
    TFreakoutSystemFactory _factory;

    protected override void SetUp()
    {
        base.SetUp();

        _factory = Using(new TFreakoutSystemFactory());
    }

    [Test]
    public async Task CanStartUpAndShutDown()
    {
        var (outbox, commandStore) = await _factory.CreateAsync();
    }

    [Test]
    public async Task CanSendAndReceiveSingleCommand()
    {
        var (outbox, commandStore) = await _factory.CreateAsync();

        await outbox.AddOutboxCommandAsync(new SomeKindOfCommand());

        using var batch = await commandStore.GetPendingOutboxCommandsAsync();

        Assert.That(batch.Count(), Is.EqualTo(1));

        var command = batch.First();

        Assert.That(command.Headers, Contains.Key(HeaderKeys.CommandType).WithValue(typeof(SomeKindOfCommand).GetSimpleAssemblyQualifiedName()));
    }

    record SomeKindOfCommand;
}