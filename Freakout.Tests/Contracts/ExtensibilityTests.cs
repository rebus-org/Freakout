using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Polly;
using Polly.Retry;
using Testy;
using Testy.Extensions;
using Testy.General;
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable AccessToDisposedClosure

namespace Freakout.Tests.Contracts;

public abstract class ExtensibilityTests<TFreakoutSystemFactory> : FixtureBase where TFreakoutSystemFactory : IFreakoutSystemFactory, new()
{
    TFreakoutSystemFactory _factory;

    protected override void SetUp()
    {
        base.SetUp();

        _factory = Using(new TFreakoutSystemFactory());
    }

    [Test]
    public async Task WorksWithPolly()
    {
        using var done = new ManualResetEvent(initialState: false);

        var stop = Using(new CancellationTokenSource());

        Using(new DisposableCallback(stop.Cancel));

        var system = _factory.Create(after: services =>
        {
            services.AddSingleton(done);
            services.Decorate<ICommandDispatcher, PollyCommandDispatcher>();
        });

        _ = system.StartCommandProcessorAsync(stop.Token);

        using (system.CreateScope())
        {
            await system.Outbox.AddOutboxCommandAsync("HEJ", cancellationToken: stop.Token);
        }

        if (!done.WaitOne(TimeSpan.FromSeconds(5)))
        {
            throw new AssertionException("command was not handled within 5 s");
        }
    }

    class ThrowingCommandHandler(ManualResetEvent done) : ICommandHandler<string>
    {
        public async Task HandleAsync(string command, CancellationToken cancellationToken)
        {
            done.Set();
        }
    }

    class PollyCommandDispatcher(ICommandDispatcher commandDispatcher) : ICommandDispatcher
    {
        readonly AsyncRetryPolicy RetryPolicy = Policy.Handle<Exception>(e => e is not OperationCanceledException)
            .WaitAndRetryAsync(10, n => TimeSpan.FromMilliseconds(n * 2));

        public async Task ExecuteAsync(OutboxCommand outboxCommand, CancellationToken cancellationToken = default)
        {
            await RetryPolicy.ExecuteAsync(
                action: token => commandDispatcher.ExecuteAsync(outboxCommand, token),
                cancellationToken: cancellationToken
            );
        }
    }
}