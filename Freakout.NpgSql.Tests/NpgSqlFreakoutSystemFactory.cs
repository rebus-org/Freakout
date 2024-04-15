using Freakout.Config;
using Freakout.Tests.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Nito.Disposables;
using Npgsql;
using Testy.General;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Freakout.NpgSql.Tests;

public class NpgSqlFreakoutSystemFactory : IFreakoutSystemFactory
{
    readonly CollectionDisposable disposables = new();

    public async Task<FreakoutSystem> CreateAsync()
    {
        var services = new ServiceCollection();

        var tableName = $"outbox-{Guid.NewGuid():N}";

        disposables.Add(new DisposableCallback(() => NpgSqlTestHelper.DropTable(tableName)));

        var configuration = new NpgSqlFreakoutConfiguration(NpgSqlTestHelper.ConnectionString)
        {
            OutboxPollInterval = TimeSpan.FromSeconds(1),
        };

        services.AddFreakout(configuration);

        var provider = services.BuildServiceProvider();

        disposables.Add(provider);

        IFreakoutContext ContextFactory()
        {
            var connection = new NpgsqlConnection(NpgSqlTestHelper.ConnectionString);
            connection.Open();
            var transaction = connection.BeginTransaction();
            return new NpgsqlFreakoutContext(connection, transaction);
        }

        void CommitAction(IFreakoutContext context)
        {
            var ctx = (NpgsqlFreakoutContext)context;
            ctx.Transaction.Commit();
        }

        void DisposeAction(IFreakoutContext context)
        {
            var ctx = (NpgsqlFreakoutContext)context;
            ctx.Transaction.Dispose();
            ctx.Connection.Dispose();
        }

        return new(provider, ContextFactory, CommitAction, DisposeAction);
    }

    public void Dispose() => disposables.Dispose();
}