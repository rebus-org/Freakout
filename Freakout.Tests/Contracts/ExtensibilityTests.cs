using System;
using System.Threading;
using System.Threading.Tasks;
using Freakout.Internals;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Polly;
using Polly.Retry;
using Testy;
using Testy.General;
// ReSharper disable ClassNeverInstantiated.Local

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
        var stop = Using(new CancellationTokenSource());

        Using(new DisposableCallback(() => stop.Cancel()));

        var system = _factory.Create(after: services =>
        {
            services.Decorate<ICommandDispatcher, PollyCommandDispatcher>();
        });

        _ = system.StartCommandProcessorAsync(stop.Token);

    }

    class PollyCommandDispatcher(ICommandDispatcher commandDispatcher) : ICommandDispatcher
    {
        static readonly AsyncRetryPolicy RetryPolicy = Policy.Handle<Exception>(e => e is not OperationCanceledException)
            .WaitAndRetryAsync(10, n => TimeSpan.FromSeconds(n * 2));

        public async Task ExecuteAsync(OutboxCommand outboxCommand, CancellationToken cancellationToken = default)
        {
            await RetryPolicy.ExecuteAsync(
                action: token => commandDispatcher.ExecuteAsync(outboxCommand, token),
                cancellationToken: cancellationToken
            );
        }
    }
}