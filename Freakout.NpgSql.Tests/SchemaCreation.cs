using Freakout.Config;
using Freakout.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Testy;
using Testy.General;

namespace Freakout.NpgSql.Tests;

[TestFixture]
public class SchemaCreation : FixtureBase
{
    string _connectionString;
    CancellationTokenSource _cancellationTokenSource;

    protected override void SetUp()
    {
        Using(new DisposableCallback(() => NpgsqlTestHelper.DropTable("custom", "OutboxCommands")));

        base.SetUp();

        _connectionString = NpgsqlTestHelper.ConnectionString;

        NpgsqlTestHelper.DropTable("custom", "OutboxCommands");

        _cancellationTokenSource = Using(new CancellationTokenSource());

        Using(new DisposableCallback(_cancellationTokenSource.Cancel));
    }

    [Test]
    public async Task ItWorks()
    {
        var services = new ServiceCollection();

        // normal stuff
        services.AddLogging(l => l.AddConsole());

        // freakout stuff
        var configuration = new NpgsqlFreakoutConfiguration(_connectionString)
        {
            OutboxPollInterval = TimeSpan.FromSeconds(1),
            SchemaName = "custom",
            TableName = "OutboxCommands"
        };

        services.AddFreakout(configuration);

        _cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));

        // it must be possible to build the service provider, run the background services, and get available commands
        await using var provider = services.BuildServiceProvider();
        _ = provider.RunBackgroundWorkersAsync(_cancellationTokenSource.Token);

        var store = provider.GetRequiredService<IOutboxCommandStore>();
        _ = await store.GetPendingOutboxCommandsAsync(commandProcessingBatchSize: 1);

        await _cancellationTokenSource.CancelAsync();
    }
}