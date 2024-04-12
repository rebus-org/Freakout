using System;
using System.Threading;
using System.Threading.Tasks;
using Freakout.Config;
using Freakout.Internals.Dispatch;
using Freakout.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Testy;

namespace Freakout.Tests.Dispatch;

[TestFixture]
public class TestFreakoutDispatcher : FixtureBase
{
    FreakoutDispatcher _dispatcher;
    SystemTextJsonCommandSerializer _serializer;

    protected override void SetUp()
    {
        base.SetUp();

        _serializer = new SystemTextJsonCommandSerializer();
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
    [TestCase(1000000)]
    [TestCase(10000000)]
    public async Task TakeTime(int count)
    {
        var services = new ServiceCollection();

        services.AddCommandHandler<SomeCommand, SomeCommandHandler>();

        await using var provider = services.BuildServiceProvider();

        _dispatcher = new FreakoutDispatcher(_serializer, provider.GetRequiredService<IServiceScopeFactory>());

        using var _ = TimerScope($"Dispatch {count} commands", count);

        for (var counter = 0; counter < count; counter++)
        {
            var command = new SomeCommand();
            var serializedCommand = _serializer.Serialize(command);
            var outboxCommand = GetOutboxCommand(serializedCommand);

            await _dispatcher.ExecuteAsync(outboxCommand, CancellationToken.None);
        }
    }

    OutboxCommand GetOutboxCommand(SerializedCommand serializedCommand) =>
        new(
            Time: DateTimeOffset.Now,
            Headers: new() { [HeaderKeys.Type] = serializedCommand.TypeHeader, },
            Payload: serializedCommand.Payload
        );

    record SomeCommand;

    class SomeCommandHandler : ICommandHandler<SomeCommand>
    {
        public Task HandleAsync(SomeCommand command, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}