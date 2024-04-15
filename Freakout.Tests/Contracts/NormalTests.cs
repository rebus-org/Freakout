using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Freakout.Config;
using Freakout.Internals;
using Microsoft.Extensions.DependencyInjection;
using Nito.AsyncEx;
using NUnit.Framework;
using Testy;
using Testy.Extensions;
using Testy.General;
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable ConvertToUsingDeclaration
// ReSharper disable MethodSupportsCancellation

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

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
    public void CanStartUpAndShutDown() => _ = _factory.Create();

    [Test]
    [Description("To provide the best experience, Freakout registers a couple of global objects. We want to be pretty sure that they're there when the system runs, and that they're gone when it's stopped again.")]
    public void CanStartUpAndShutDown_CheckGlobals()
    {
        var globalsBefore = Globals.GetAll();

        _ = _factory.Create();

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
        var system = _factory.Create();
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
        var system = _factory.Create();
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
        var system = _factory.Create();
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

    [Test]
    public async Task HandlersAreExecutedInScope()
    {
        var events = new ConcurrentQueue<string>();

        var services = new ServiceCollection();

        services.AddCommandHandler<SomeKindOfCommandHandler>();
        services.AddSingleton(events);

        var system = _factory.Create(services);

        var cts = Using(new CancellationTokenSource());
        Using(new DisposableCallback(cts.Cancel));

        _ = system.StartCommandProcessorAsync(cts.Token);

        var expectedNameOfContext = "";

        _ = Task.Run(async () =>
        {
            using var scope = system.CreateScope();

            expectedNameOfContext = new AsyncLocalFreakoutContextAccessor().GetContext<IFreakoutContext>()?.GetType().Name;

            var outbox = system.Outbox;

            await outbox.AddOutboxCommandAsync(new SomeKindOfCommand(), cancellationToken: CancellationToken.None);

            scope.Complete();
        }, CancellationToken.None);

        await events.WaitOrDie(q => q.Count == 1, failExpression: q => q.Count > 1, timeoutSeconds: 5);

        var text = events.First();

        Assert.That(text, Is.EqualTo($"Got this context: {expectedNameOfContext}"));
    }

    class SomeKindOfCommandHandler(IFreakoutContextAccessor contextAccessor, ConcurrentQueue<string> events) : ICommandHandler<SomeKindOfCommand>
    {
        public async Task HandleAsync(SomeKindOfCommand command, CancellationToken cancellationToken)
        {
            var context = contextAccessor.GetContext<IFreakoutContext>(throwIfNull: false);

            events.Enqueue($"Got this context: {context?.GetType().Name}");
        }
    }

    [Test]
    public void CommandHandlerCanUseOutboxToo()
    {
        var done = new AsyncManualResetEvent();
        var events = new ConcurrentQueue<string>();
        var services = new ServiceCollection();

        services.AddSingleton(done);
        services.AddSingleton(events);
        services.AddCommandHandler<CommandHandler>();

        var system = _factory.Create(services);

        var cts = Using(new CancellationTokenSource());
        Using(new DisposableCallback(cts.Cancel));

        _ = system.StartCommandProcessorAsync(cts.Token);

        using (var scope = system.CreateScope())
        {
            var outbox = system.Outbox;
            outbox.AddOutboxCommand(new InitiatingCommand());
            scope.Complete();
        }

        done.Wait(CancelAfter(TimeSpan.FromSeconds(5)));

        Assert.That(events, Is.EqualTo(new[]
        {
            "Got InitiatingCommand",
            "Sent FinishingCommand",
            "Got FinishingCommand",
            "Signaling done!"
        }));
    }

    record InitiatingCommand;

    class CommandHandler(IOutbox outbox, ConcurrentQueue<string> events, AsyncManualResetEvent done) : ICommandHandler<InitiatingCommand>, ICommandHandler<FinishingCommand>
    {
        public async Task HandleAsync(InitiatingCommand command, CancellationToken cancellationToken)
        {
            events.Enqueue("Got InitiatingCommand");
            await outbox.AddOutboxCommandAsync(new FinishingCommand(), cancellationToken: cancellationToken);
            events.Enqueue("Sent FinishingCommand");
        }

        public async Task HandleAsync(FinishingCommand command, CancellationToken cancellationToken)
        {
            events.Enqueue("Got FinishingCommand");
            events.Enqueue("Signaling done!");
            done.Set();
        }
    }

    record FinishingCommand;
}