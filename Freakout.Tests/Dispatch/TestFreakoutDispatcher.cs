using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Freakout.Config;
using Freakout.Internals.Dispatch;
using Freakout.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Testy;
// ReSharper disable ClassNeverInstantiated.Local
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Freakout.Tests.Dispatch;

[TestFixture]
public class TestFreakoutDispatcher : FixtureBase
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

        var dispatcher = new FreakoutDispatcher(_serializer, provider.GetRequiredService<IServiceScopeFactory>());

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

        var dispatcher = new FreakoutDispatcher(_serializer, provider.GetRequiredService<IServiceScopeFactory>());

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
              
     *
     */
    [TestCase(1000)]
    [TestCase(1000000, Explicit = true)]
    [TestCase(10000000, Explicit = true)]
    public async Task TakeTime(int count)
    {
        var services = new ServiceCollection();

        services.AddCommandHandler<SomeCommandHandler>();

        await using var provider = services.BuildServiceProvider();

        var dispatcher = new FreakoutDispatcher(_serializer, provider.GetRequiredService<IServiceScopeFactory>());

        using var _ = TimerScope($"Dispatch {count} commands", count);

        for (var counter = 0; counter < count; counter++)
        {
            var command = new SomeCommand();
            var outboxCommand = GetOutboxCommand(command);

            await dispatcher.ExecuteAsync(outboxCommand, CancellationToken.None);
        }
    }

    OutboxCommand GetOutboxCommand(object command)
    {
        var serializedCommand = _serializer.Serialize(command);
        var headers = new Dictionary<string, string>() { [HeaderKeys.Type] = serializedCommand.TypeHeader, };
        var payload = serializedCommand.Payload;

        return new OutboxCommand(
            Time: DateTimeOffset.Now,
            Headers: headers,
            Payload: payload
        );
    }

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