using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Freakout.Config;
using Freakout.MsSql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Testy.Extensions;
using Testy.General;
using CancellationTokenSource = System.Threading.CancellationTokenSource;
// ReSharper disable AccessToDisposedClosure
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

// ReSharper disable ClassNeverInstantiated.Local
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Freakout.Tests;

[TestFixture]
public class SimpleSqlServerPoc : SqlServerFixtureBase
{
    string _connectionString;
    CancellationTokenSource _cancellationTokenSource;

    protected override void SetUp()
    {
        base.SetUp();

        _connectionString = ConnectionString;
        _cancellationTokenSource = Using(new CancellationTokenSource());
        Using(new DisposableCallback(_cancellationTokenSource.Cancel));
    }

    [Test]
    public async Task CanAppendTextInBackground()
    {
        var texts = new ConcurrentQueue<string>();

        var services = new ServiceCollection();

        services.AddLogging(l => l.AddConsole());
        services.AddSingleton(texts);
        services.AddFreakout(new MsSqlFreakoutConfiguration(_connectionString));
        services.AddFreakoutHandler<AppendTextOutboxCommand, AppendTextOutboxCommandHandler>();

        await using var provider = services.BuildServiceProvider();

        provider.RunBackgroundWorkersAsync(_cancellationTokenSource.Token);

        await AddOutboxCommandAsync(new AppendTextOutboxCommand(Text: "Howdy!"));

        await texts.WaitOrDie(
            completionExpression: t => t.Count == 1,
            failExpression: t => t.Count > 1,
            failureDetailsFunction: () =>
                "Text was not appended as expected - expected one single 'Howdy!' to have been appended"
        );

        Assert.That(texts, Is.EqualTo(new[] { "Howdy!" }));
    }

    async Task AddOutboxCommandAsync(object command)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();
        await connection.AddOutboxCommandAsync(command);
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