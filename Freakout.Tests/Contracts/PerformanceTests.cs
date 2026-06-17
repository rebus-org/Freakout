using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Freakout.Config;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Testy;
using Testy.Extensions;

// ReSharper disable AsyncMethodWithoutAwait

namespace Freakout.Tests.Contracts;

public abstract class PerformanceTests<TFreakoutSystemFactory> : FixtureBase where TFreakoutSystemFactory : IFreakoutSystemFactory, new()
{
    TFreakoutSystemFactory _factory;

    protected override void SetUp()
    {
        base.SetUp();

        _factory = Using(new TFreakoutSystemFactory());
    }

    [TestCase(10)]
    [TestCase(100)]
    public async Task RunTest(int count)
    {
        var stop = Using(disposable: new CancellationTokenSource());

        var messageIds = new ConcurrentDictionary<int, bool>();

        var system = _factory.Create(
            before: services => services
                .AddSingleton(implementationInstance: messageIds)
                .AddCommandHandler<IdentifyableCommandHandler>()
        );

        _ = system.StartCommandProcessorAsync(stoppingToken: stop.Token);

        var outbox = system.Outbox;

        var commands = Enumerable.Range(start: 0, count: count).Select(selector: id => new IdentifyableCommand(Id: id));

        using var scope = system.CreateScope();

        foreach (var command in commands)
        {
            messageIds[command.Id] = false;
            await outbox.AddOutboxCommandAsync(command: command, cancellationToken: CancellationToken.None);
        }

        scope.Complete();

        var timeoutSeconds = count * 2;

        await messageIds.WaitOrDie(
            completionExpression: m => m.All(kvp => kvp.Value == true),
            timeoutSeconds: timeoutSeconds,
            failureDetailsFunction: () => $"{messageIds.Count(kvp => !kvp.Value)} messages were not completed within the timeout of {timeoutSeconds} s!"
        );
    }

    class IdentifyableCommandHandler(ConcurrentDictionary<int, bool> messages) : ICommandHandler<IdentifyableCommand>
    {
        public async Task HandleAsync(IdentifyableCommand command, IDictionary<string, string> headers, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Handling command with ID {command.Id}");
            messages[command.Id] = true;
        }
    }

    record IdentifyableCommand(int Id);
}

