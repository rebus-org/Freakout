﻿using System;
using System.Collections.Concurrent;
using System.Linq;
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

        using var batch = await commandStore.GetPendingOutboxCommandsAsync(commandProcessingBatchSize: 1);

        Assert.That(batch.Count(), Is.EqualTo(1),
            "The command store must return a batch containing exactly 1 command at this point, because one single command has been added");

        var command = batch.First();

        Assert.That(command.Headers, Contains.Key(HeaderKeys.CommandType).WithValue(typeof(SomeKindOfCommand).GetSimpleAssemblyQualifiedName()),
            "The headers must contain type information");
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

        using var batch1 = await commandStore.GetPendingOutboxCommandsAsync(commandProcessingBatchSize: 1);
        await batch1.CompleteAsync();

        using var batch2 = await commandStore.GetPendingOutboxCommandsAsync(commandProcessingBatchSize: 1);
        await batch2.CompleteAsync();

        using var batch3 = await commandStore.GetPendingOutboxCommandsAsync(commandProcessingBatchSize: 1);
        await batch3.CompleteAsync();

        Assert.That(batch1.Count(), Is.EqualTo(1), "Expected batch to contain 1 command (because processing batch size = 1 and we added 2 commands)");
        Assert.That(batch2.Count(), Is.EqualTo(1), "Expected batch to contain 1 command (because processing batch size = 1 and we added 2 commands and have removed 1)");
        Assert.That(batch3.Count(), Is.EqualTo(0), "Expected batch to contain 0 commands (because processing batch size = 1 and we added 2 commands and have removed 2)");
    }

    [Test]
    public async Task CanSendAndReceiveMultipleCommands_ChangeOrderToDetectLockingIssues()
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

        using var batch1 = await commandStore.GetPendingOutboxCommandsAsync(commandProcessingBatchSize: 1);
        using var batch2 = await commandStore.GetPendingOutboxCommandsAsync(commandProcessingBatchSize: 1);
        using var batch3 = await commandStore.GetPendingOutboxCommandsAsync(commandProcessingBatchSize: 1);

        await batch3.CompleteAsync();
        await batch2.CompleteAsync();
        await batch1.CompleteAsync();

        Assert.That(batch1.Count(), Is.EqualTo(1), "Expected batch to contain 1 command (because processing batch size = 1 and we added 2 commands)");
        Assert.That(batch2.Count(), Is.EqualTo(1), "Expected batch to contain 1 command (because processing batch size = 1 and we added 2 commands and have removed 1)");
        Assert.That(batch3.Count(), Is.EqualTo(0), "Expected batch to contain 0 commands (because processing batch size = 1 and we added 2 commands and have removed 2)");
    }

    record SomeKindOfCommand;

    [Test]
    public async Task HandlersAreExecutedInScope()
    {
        var events = new ConcurrentQueue<string>();

        var system = _factory.Create(before: services =>
        {
            services.AddCommandHandler<SomeKindOfCommandHandler>();
            services.AddSingleton(events);
        });

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

        await events.WaitOrDie(
            completionExpression: q => q.Count == 1,
            failExpression: q => q.Count > 1,
            timeoutSeconds: 5,
            failureDetailsFunction: () => "Expected that the command handler would have appended a text like 'Got this context: <type-name>'"
        );

        var text = events.First();

        Assert.That(text, Is.EqualTo($"Got this context: {expectedNameOfContext}"),
            $"This was the event text generated by the command handler. If the type name was different from '{expectedNameOfContext}', it's a sign that the context accessor did not find the expected Freakout context");
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

        var system = _factory.Create(before: services =>
        {
            services.AddSingleton(done);
            services.AddSingleton(events);
            services.AddCommandHandler<CommandHandler>();
        });

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