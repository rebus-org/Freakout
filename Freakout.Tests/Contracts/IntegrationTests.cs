using Freakout.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Testy;

namespace Freakout.Tests.Contracts;

public abstract class IntegrationTests<TFreakoutSystemFactory> : FixtureBase where TFreakoutSystemFactory : IFreakoutSystemFactory, new()
{
    TFreakoutSystemFactory _factory;

    protected override void SetUp()
    {
        base.SetUp();

        _factory = Using(new TFreakoutSystemFactory());
    }

    [Test]
    public async Task PlainOldStartStopTest()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSimpleConsole();
        });

        services.AddFreakout(_factory.GetConfiguration());

        await using var provider = services.BuildServiceProvider();

        await provider.RunBackgroundWorkersAsync(CancelAfter(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public async Task TestCommandProcessingTimeout()
    {
        using var completed = new ManualResetEvent(initialState: false);

        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSimpleConsole();
        });

        services.AddSingleton(completed);
        services.AddSingleton<HandlerExecutionState>();
        services.AddFreakout(_factory.GetConfiguration(config =>
        {
            config.OutboxPollInterval = TimeSpan.FromSeconds(1);
            config.CommandProcessingTimeout = TimeSpan.FromSeconds(3);
        }));
        services.AddCommandHandler<HandlerThatTakesTooLongTheFirstTime>();

        using var globalTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        await using var provider = services.BuildServiceProvider();

        try
        {
            _ = provider.RunBackgroundWorkersAsync(globalTimeout.Token);

            using (var wrapper = _factory.CreateContextWrapper())
            using (new FreakoutContextScope(wrapper.Context))
            {
                var outbox = provider.GetRequiredService<IOutbox>();

                await outbox.AddOutboxCommandAsync(new EternalCommand(), cancellationToken: globalTimeout.Token);

                await wrapper.CompleteAsync(globalTimeout.Token);
            }

            // 1s poll interval + 3s command execution timeout - 6s ought to be enough to get the timeout and redeliver
            if (!completed.WaitOne(TimeSpan.FromSeconds(6)))
            {
                throw new AssertionException("Completed reset event not signaled within 10 s");
            }
        }
        finally
        {
            await globalTimeout.CancelAsync();
        }
    }

    record EternalCommand;

    class HandlerThatTakesTooLongTheFirstTime(ManualResetEvent completed, HandlerExecutionState state, ILogger<HandlerThatTakesTooLongTheFirstTime> logger) : ICommandHandler<EternalCommand>
    {
        public async Task HandleAsync(EternalCommand command, IDictionary<string, string> headers, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref state.ExecutionCount);

            var count = Volatile.Read(ref state.ExecutionCount);

            logger.LogInformation("Execution count: {count}", count);

            // if it's the first time, wait FOREVERMAN
            if (count == 1)
            {
                logger.LogInformation("Waiting forever.... (i.e until cancelled)");
                await Task.Delay(-1, cancellationToken);
            }

            logger.LogInformation("Successfully completed execution");
            completed.Set();
        }
    }

    class HandlerExecutionState
    {
        public int ExecutionCount = 0;
    }
}