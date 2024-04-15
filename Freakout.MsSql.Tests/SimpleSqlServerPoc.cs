using System.Collections.Concurrent;
using Freakout.Config;
using Freakout.Tests;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Testy;
using Testy.Extensions;
using Testy.General;
using CancellationTokenSource = System.Threading.CancellationTokenSource;
// ReSharper disable AccessToDisposedClosure
// ReSharper disable ClassNeverInstantiated.Local
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Freakout.MsSql.Tests;

[TestFixture]
public class SimpleSqlServerPoc : FixtureBase
{
    string _connectionString;
    CancellationTokenSource _cancellationTokenSource;

    protected override void SetUp()
    {
        Using(new DisposableCallback(() => MsSqlTestHelper.DropTable("OutboxCommands")));

        base.SetUp();

        Using(new GlobalsCleaner());

        _connectionString = MsSqlTestHelper.ConnectionString;

        MsSqlTestHelper.DropTable("OutboxCommands");

        _cancellationTokenSource = Using(new CancellationTokenSource());

        Using(new DisposableCallback(_cancellationTokenSource.Cancel));
    }

    [Test]
    public async Task CanAppendTextInBackground()
    {
        var texts = new ConcurrentQueue<string>();

        var services = new ServiceCollection();

        // normal stuff
        services.AddLogging(l => l.AddConsole());
        services.AddSingleton(texts);

        // freakout stuff
        services.AddFreakout(new MsSqlFreakoutConfiguration(_connectionString) { OutboxPollInterval = TimeSpan.FromSeconds(1) });
        services.AddCommandHandler<AppendTextOutboxCommandHandler>();

        await using var provider = services.BuildServiceProvider();

        provider.RunBackgroundWorkersAsync(_cancellationTokenSource.Token);

        // pretend something happens somewhere else
        Task.Run(async () => await AddOutboxCommandAsync(new AppendTextOutboxCommand(Text: "Howdy!")));

        await texts.WaitOrDie(
            completionExpression: t => t.Count == 1,
            failExpression: t => t.Count > 1,
            failureDetailsFunction: () =>
                $"Text was not appended as expected - expected one single 'Howdy!' to have been appended, but we got this: {string.Join(", ", texts)}"
        );

        Assert.That(texts, Is.EqualTo(new[] { "Howdy!" }));
    }

    [Test]
    public async Task CanAppendTextInBackground_UsingScopedOutboxAppender()
    {
        var texts = new ConcurrentQueue<string>();

        var services = new ServiceCollection();

        // normal stuff
        services.AddLogging(l => l.AddConsole());
        services.AddSingleton(texts);

        // freakout stuff
        services.AddFreakout(new MsSqlFreakoutConfiguration(_connectionString) { OutboxPollInterval = TimeSpan.FromSeconds(1) });
        services.AddCommandHandler<AppendTextOutboxCommandHandler>();

        await using var provider = services.BuildServiceProvider();

        provider.RunBackgroundWorkersAsync(_cancellationTokenSource.Token);

        // pretend something happens somewhere else
        Task.Run(async () =>
        {
            try
            {
                await AddOutboxCommandUsingScopedOutboxAppenderAsync(provider,
                    new AppendTextOutboxCommand(Text: "Howdy!"));
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        });

        await texts.WaitOrDie(
            timeoutSeconds: 3,
            completionExpression: t => t.Count == 1,
            failExpression: t => t.Count > 1,
            failureDetailsFunction: () =>
                $"Text was not appended as expected - expected one single 'Howdy!' to have been appended, but we got this: {string.Join(", ", texts)}"
        );

        Assert.That(texts, Is.EqualTo(new[] { "Howdy!" }));
    }

    static async Task AddOutboxCommandUsingScopedOutboxAppenderAsync(ServiceProvider provider, object command)
    {
        using var scope = provider.GetRequiredService<IServiceScopeFactory>().CreateScope();

        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();

        await outbox.AddOutboxCommandAsync(command);
    }

    async Task AddOutboxCommandAsync(object command)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();
        await transaction.AddOutboxCommandAsync(command);
        await transaction.CommitAsync();
    }

    /// <summary>
    /// This is a command
    /// </summary>
    record AppendTextOutboxCommand(string Text);

    /// <summary>
    /// This is a command handler
    /// </summary>
    class AppendTextOutboxCommandHandler(ConcurrentQueue<string> texts) : ICommandHandler<AppendTextOutboxCommand>
    {
        public async Task HandleAsync(AppendTextOutboxCommand command, CancellationToken cancellationToken) => texts.Enqueue(command.Text);
    }
}