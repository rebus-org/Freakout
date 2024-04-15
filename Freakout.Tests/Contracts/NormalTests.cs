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
    public void CanStartUpAndShutDown() => _ = _factory.CreateAsync();

    [Test]
    [Description("To provide the best experience, Freakout registers a couple of global objects. We want to be pretty sure that they're there when the system runs, and that they're gone when it's stopped again.")]
    public void CanStartUpAndShutDown_CheckGlobals()
    {
        var globalsBefore = Globals.GetAll();

        _ = _factory.CreateAsync();

        var detectedPresenceOfConfiguration = Globals.Get<FreakoutConfiguration>() != null;

        _factory.Dispose();

        var globalsAfterShutdown = Globals.GetAll();

        Assert.That(globalsBefore.Length, Is.EqualTo(0), "Expected 0 globals when we haven't started the system");
        Assert.That(globalsAfterShutdown.Length, Is.EqualTo(0), "Expected 0 globals after shutting down the system");
        Assert.That(detectedPresenceOfConfiguration, Is.True, "Did not find a global FreakoutConfiguration object");
    }

    [Test]
    public async Task CanSendAndReceiveSingleCommand()
    {
        var system = _factory.CreateAsync();
        var commandStore = system.OutboxCommandStore;
        var outbox = system.Outbox;

        using (var scope = system.CreateScope())
        {
            await outbox.AddOutboxCommandAsync(new SomeKindOfCommand());
            scope.Complete();
        }

        using var batch = await commandStore.GetPendingOutboxCommandsAsync();

        Assert.That(batch.Count(), Is.EqualTo(1));

        var command = batch.First();

        Assert.That(command.Headers, Contains.Key(HeaderKeys.CommandType).WithValue(typeof(SomeKindOfCommand).GetSimpleAssemblyQualifiedName()));
    }

    [Test]
    public async Task CanSendAndReceiveMultipleCommands()
    {
        var system = _factory.CreateAsync();
        var commandStore = system.OutboxCommandStore;
        var outbox = system.Outbox;

        using (var scope = system.CreateScope())
        {
            await outbox.AddOutboxCommandAsync(new SomeKindOfCommand());
            await outbox.AddOutboxCommandAsync(new SomeKindOfCommand());
            scope.Complete();
        }

        using var batch1 = await commandStore.GetPendingOutboxCommandsAsync();
        await batch1.CompleteAsync();

        using var batch2 = await commandStore.GetPendingOutboxCommandsAsync();
        await batch2.CompleteAsync();

        using var batch3 = await commandStore.GetPendingOutboxCommandsAsync();
        await batch3.CompleteAsync();

        Assert.That(batch1.Count(), Is.EqualTo(1));
        Assert.That(batch2.Count(), Is.EqualTo(1));
        Assert.That(batch3.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task CanSendAndReceiveMultipleCommandsParallel()
    {
        var system = _factory.CreateAsync();
        var commandStore = system.OutboxCommandStore;
        var outbox = system.Outbox;

        using (var scope = system.CreateScope())
        {
            await outbox.AddOutboxCommandAsync(new SomeKindOfCommand());
            await outbox.AddOutboxCommandAsync(new SomeKindOfCommand());
            scope.Complete();
        }

        using var batch1 = await commandStore.GetPendingOutboxCommandsAsync();
        using var batch2 = await commandStore.GetPendingOutboxCommandsAsync();
        using var batch3 = await commandStore.GetPendingOutboxCommandsAsync();

        await batch3.CompleteAsync();
        await batch2.CompleteAsync();
        await batch1.CompleteAsync();

        Assert.That(batch1.Count(), Is.EqualTo(1));
        Assert.That(batch2.Count(), Is.EqualTo(1));
        Assert.That(batch3.Count(), Is.EqualTo(0));
    }

    record SomeKindOfCommand;
}