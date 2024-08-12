using Freakout.Config;
using Freakout.Tests;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Testy;

// ReSharper disable ClassNeverInstantiated.Local
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Freakout.MsSql.Tests;

[TestFixture]
public class ThisIsWhatWeWant : FixtureBase
{
    string _connectionString;

    protected override void SetUp()
    {
        base.SetUp();

        // we have a database
        _connectionString = MsSqlTestHelper.ConnectionString;
    }

    [Test]
    public async Task MakeItLookPrettyLikeThis()
    {
        var configuration = new MsSqlFreakoutConfiguration(_connectionString);

        // and a modern .NET app
        var services = new ServiceCollection();
        services.AddFreakout(configuration);
        services.AddCommandHandler<PrintTextOutboxCommandHandler>();

        await using var provider = services.BuildServiceProvider();

        using var cancellationTokenSource = new CancellationTokenSource();
        _ = provider.RunBackgroundWorkersAsync(cancellationTokenSource.Token);

        // in our app we sometimes execute SQL stuff like this
        await AddOutboxCommandAsync(configuration, new PrintTextOutboxCommand(Text: "Howdy!"));

        await cancellationTokenSource.CancelAsync();
    }

    async Task AddOutboxCommandAsync(MsSqlFreakoutConfiguration configuration, object command)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();
        await transaction.AddOutboxCommandAsync(configuration.CommandSerializer, configuration.SchemaName, configuration.TableName, command);
        await transaction.CommitAsync();
    }

    /// <summary>
    /// This is a command
    /// </summary>
    record PrintTextOutboxCommand(string Text);

    /// <summary>
    /// This is a command handler
    /// </summary>
    class PrintTextOutboxCommandHandler : ICommandHandler<PrintTextOutboxCommand>
    {
        public async Task HandleAsync(PrintTextOutboxCommand command, CancellationToken cancellationToken)
        {
            Console.WriteLine(command.Text);
        }
    }
}