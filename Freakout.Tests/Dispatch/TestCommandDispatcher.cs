using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Freakout.Config;
using Freakout.Internals.Dispatchers;
using Freakout.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Testy;
// ReSharper disable ClassNeverInstantiated.Local
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Freakout.Tests.Dispatch;

[TestFixture]
public class TestCommandDispatcher : FixtureBase
{
    SystemTextJsonCommandSerializer _serializer;

    protected override void SetUp()
    {
        base.SetUp();

        _serializer = new SystemTextJsonCommandSerializer();
    }

    [Test]
    public async Task CanDispatchStuff_GetNiceErrorWhenCommandHandlerIsNotRegistered()
    {
        var services = new ServiceCollection();

        await using var provider = services.BuildServiceProvider();

        var dispatcher = new CompiledExpressionCommandDispatcher(_serializer, provider.GetRequiredService<IServiceScopeFactory>());

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.ExecuteAsync(GetOutboxCommand(new SomeCommand())));

        Console.WriteLine(ex);
    }

    [Test]
    public async Task CanDispatchStuff()
    {
        var events = new ConcurrentQueue<string>();
        var services = new ServiceCollection();

        services.AddSingleton(events);
        services.AddCommandHandler<AnotherCommandHandler>();
        services.AddCommandHandler<ThirdCommandHandler>();

        await using var provider = services.BuildServiceProvider();

        var dispatcher = new CompiledExpressionCommandDispatcher(_serializer, provider.GetRequiredService<IServiceScopeFactory>());

        await dispatcher.ExecuteAsync(GetOutboxCommand(new AnotherCommand("hej")));
        await dispatcher.ExecuteAsync(GetOutboxCommand(new AnotherCommand("hej med dig")));
        await dispatcher.ExecuteAsync(GetOutboxCommand(new ThirdCommand("hej")));
        await dispatcher.ExecuteAsync(GetOutboxCommand(new ThirdCommand("hej med dig")));

        Assert.That(events, Is.EqualTo(new[]
        {
            "AnotherCommandHandler called - text: hej",
            "AnotherCommandHandler called - text: hej med dig",
            "ThirdCommandHandler called - text: hej",
            "ThirdCommandHandler called - text: hej med dig",
        }));
    }

    /*
    
    Initial runs:

SCOPE 'Dispatch 1000 commands' completed in 31,196 ms | 0,031196 ms/item | 32,05539171688678 items/ms
SCOPE 'Dispatch 1000000 commands' completed in 4438,4956 ms | 0,0044384956 ms/item | 225,30156389025146 items/ms
SCOPE 'Dispatch 10000000 commands' completed in 37986,2051 ms | 0,00379862051 ms/item | 263,2534619784907 items/ms

    After storing closed generic methods in concurrent dictionary:

SCOPE 'Dispatch 1000 commands' completed in 37,0173 ms | 0,037017299999999996 ms/item | 27,014395971613272 items/ms
SCOPE 'Dispatch 1000000 commands' completed in 3731,3556 ms | 0,0037313555999999998 ms/item | 267,99911538851995 items/ms
SCOPE 'Dispatch 10000000 commands' completed in 31529,0346 ms | 0,0031529034599999998 ms/item | 317,1679731671835 items/ms
        
    After using ChatGPT to convert into expression tree:

SCOPE 'Dispatch 1000 commands' completed in 29,3767 ms | 0,0293767 ms/item | 34,040583183271096 items/ms
SCOPE 'Dispatch 1000000 commands' completed in 3405,0657 ms | 0,0034050657 ms/item | 293,68008963821165 items/ms
SCOPE 'Dispatch 10000000 commands' completed in 30029,7693 ms | 0,00300297693 ms/item | 333,0028912343326 items/ms

    After waiting for the 29th of May 2024 to come by:

SCOPE 'Dispatch 1000 commands' completed in 31,8928 ms | 0,0318928 ms/item | 31,355039381929462 items/ms
SCOPE 'Dispatch 1000000 commands' completed in 3675,3738 ms | 0,0036753738 ms/item | 272,08116899565425 items/ms              
SCOPE 'Dispatch 10000000 commands' completed in 29720,7503 ms | 0,00297207503 ms/item | 336,46526077102436 items/ms
       
    After switching to IL-generated dispatcher:

SCOPE 'Dispatch 1000 commands' completed in 34,6107 ms | 0,0346107 ms/item | 28,89279904769334 items/ms
SCOPE 'Dispatch 1000000 commands' completed in 3900,4479 ms | 0,0039004479 ms/item | 256,3808120600714 items/ms
SCOPE 'Dispatch 10000000 commands' completed in 35641,9579 ms | 0,00356419579 ms/item | 280,5682007721579 items/ms
       
    NOT IMPRESSED!! Switching back to the expression tree-based invoker.


    New run with expression-based invoker (this time in Release mode):

SCOPE 'Dispatch 1000 commands' completed in 30,0883 ms | 0,030088300000000002 ms/item | 33,23551014846302 items/ms
SCOPE 'Dispatch 1000000 commands' completed in 2730,8531 ms | 0,0027308530999999997 ms/item | 366,18593654854595 items/ms
SCOPE 'Dispatch 10000000 commands' completed in 23148,3344 ms | 0,00231483344 ms/item | 431,9965241214072 items/ms
    
    New run with Danielovich's modded IL invoker (also in Release mode):

SCOPE 'Dispatch 1000 commands' completed in 38,6496 ms | 0,0386496 ms/item | 25,873488988243086 items/ms
SCOPE 'Dispatch 1000000 commands' completed in 2650,4597 ms | 0,0026504596999999998 ms/item | 377,29304090154625 items/ms
SCOPE 'Dispatch 10000000 commands' completed in 22555,6927 ms | 0,00225556927 ms/item | 443,34705801343 items/ms
       

       
     *
     */
    [TestCase(1000, CommandDispatcherImplementation.CompiledExpression)]
    [TestCase(1000000, CommandDispatcherImplementation.CompiledExpression, Explicit = true)]
    [TestCase(10000000, CommandDispatcherImplementation.CompiledExpression, Explicit = true)]
    [TestCase(1000, CommandDispatcherImplementation.IlEmit)]
    [TestCase(1000000, CommandDispatcherImplementation.IlEmit, Explicit = true)]
    [TestCase(10000000, CommandDispatcherImplementation.IlEmit, Explicit = true)]
    [Repeat(5)]
    public async Task TakeTime(int count, CommandDispatcherImplementation impl)
    {
        var services = new ServiceCollection();

        services.AddCommandHandler<SomeCommandHandler>();

        await using var provider = services.BuildServiceProvider();

        var serviceScopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var dispatcher = impl switch
        {
            CommandDispatcherImplementation.CompiledExpression => (ICommandDispatcher)new CompiledExpressionCommandDispatcher(
                commandSerializer: _serializer,
                serviceScopeFactory: serviceScopeFactory
            ),

            CommandDispatcherImplementation.IlEmit => new IlEmitCommandDispatcher(
                commandSerializer: _serializer,
                serviceScopeFactory: serviceScopeFactory
            ),

            _ => throw new ArgumentOutOfRangeException(nameof(impl), impl, "Unknown command dispatcher implementation")
        };

        using var _ = TimerScope($"Dispatch {count} commands", count);

        for (var counter = 0; counter < count; counter++)
        {
            var command = new SomeCommand();
            var outboxCommand = GetOutboxCommand(command);

            await dispatcher.ExecuteAsync(outboxCommand, CancellationToken.None);
        }
    }

    public enum CommandDispatcherImplementation
    {
        CompiledExpression,
        IlEmit
    }

    OutboxCommand GetOutboxCommand(object command) => _serializer.Serialize(command);

    record SomeCommand;

    class SomeCommandHandler : ICommandHandler<SomeCommand>
    {
        public Task HandleAsync(SomeCommand command, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    record AnotherCommand(string Text);

    class AnotherCommandHandler(ConcurrentQueue<string> events) : ICommandHandler<AnotherCommand>
    {
        public async Task HandleAsync(AnotherCommand command, CancellationToken cancellationToken) => events.Enqueue($"{GetType().Name} called - text: {command.Text}");
    }

    record ThirdCommand(string Text);

    class ThirdCommandHandler(ConcurrentQueue<string> events) : ICommandHandler<ThirdCommand>
    {
        public async Task HandleAsync(ThirdCommand command, CancellationToken cancellationToken) => events.Enqueue($"{GetType().Name} called - text: {command.Text}");
    }
}