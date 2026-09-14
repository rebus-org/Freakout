using Freakout.Config;
using Freakout.Tests;
using Freakout.Tests.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testy.General;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Freakout.NpgSql.Tests;

public class NpgsqlFreakoutSystemFactory : AbstractFreakoutSystemFactory
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        var tableName = $"outbox_{Guid.NewGuid():N}";

        disposables.Add(new DisposableCallback(() => NpgsqlTestHelper.DropTable(tableName)));

        var configuration = new NpgsqlFreakoutConfiguration(NpgsqlTestHelper.ConnectionString)
        {
            OutboxPollInterval = TimeSpan.FromSeconds(1),
            TableName = tableName
        };

        services.AddFreakout(configuration);
    }

    public override FreakoutConfiguration GetConfiguration(Action<FreakoutConfiguration> configure = null)
    {
        var tableName = $"outbox_{Guid.NewGuid():N}";

        var configuration = new NpgsqlFreakoutConfiguration(NpgsqlTestHelper.ConnectionString);

        configure?.Invoke(configuration);

        disposables.Add(new DisposableCallback(() => NpgsqlTestHelper.DropTable(configuration.TableName)));

        return configuration;
    }

    public override FreakoutContextWrapper CreateContextWrapper()
    {
        var connection = new NpgsqlConnection(NpgsqlTestHelper.ConnectionString);
        connection.Open();

        try
        {
            var transaction = connection.BeginTransaction();
            var context = new NpgsqlFreakoutContext(connection, transaction);

            async Task CommitAsync(CancellationToken cancellationToken)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            void Dispose()
            {
                transaction.Dispose();
                connection.Dispose();
            }

            return new GenericFreakoutContextWrapper(context, CommitAsync, Dispose);
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    protected override FreakoutSystem GetFreakoutSystem(ServiceProvider provider)
    {
        IFreakoutContext ContextFactory()
        {
            var connection = new NpgsqlConnection(NpgsqlTestHelper.ConnectionString);
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
}