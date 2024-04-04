using System;
using System.Threading;
using System.Threading.Tasks;
using Freakout.Config;
using Freakout.MsSql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
// ReSharper disable ClassNeverInstantiated.Local
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Freakout.Tests;

[TestFixture]
public class ThisIsWhatWeWant : SqlServerFixtureBase
{
    string _connectionString;

    protected override void SetUp()
    {
        base.SetUp();

        // we have a database
        _connectionString = ConnectionString;
    }

    [Test]
    public async Task METHOD()
    {
        // and a modern .NET app
        var services = new ServiceCollection();
        services.AddFreakout(new MsSqlFreakoutConfiguration(_connectionString));
        services.AddFreakoutHandler<PrintTextOutboxCommand, PrintTextOutboxCommandHandler>();

        await using var provider = services.BuildServiceProvider();

        // in our app we sometimes execute SQL stuff like this
        await AddOutboxCommandAsync(new PrintTextOutboxCommand(Text: "Howdy!"));
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