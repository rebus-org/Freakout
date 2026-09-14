using Freakout.Config;
using Freakout.Tests;
using Freakout.Tests.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Testy.General;

namespace Freakout.MsSql.Tests;

public class MsSqlFreakoutSystemFactory : AbstractFreakoutSystemFactory
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        var tableName = $"outbox-{Guid.NewGuid():N}";

        disposables.Add(new DisposableCallback(() => MsSqlTestHelper.DropTable(tableName)));

        var configuration = new MsSqlFreakoutConfiguration(MsSqlTestHelper.ConnectionString)
        {
            TableName = tableName,
            OutboxPollInterval = TimeSpan.FromSeconds(1)
        };

        services.AddFreakout(configuration);
    }

    public override FreakoutConfiguration GetConfiguration(Action<FreakoutConfiguration> configure = null)
    {
        var tableName = $"outbox-{Guid.NewGuid():N}";

        var configuration = new MsSqlFreakoutConfiguration(MsSqlTestHelper.ConnectionString)
        {
            TableName = tableName
        };

        configure?.Invoke(configuration);

        disposables.Add(new DisposableCallback(() => MsSqlTestHelper.DropTable(configuration.TableName)));

        return configuration;
    }

    public override FreakoutContextWrapper CreateContextWrapper()
    {
        var connection = new SqlConnection(MsSqlTestHelper.ConnectionString);
        connection.Open();

        try
        {
            var transaction = connection.BeginTransaction();
            var context = new MsSqlFreakoutContext(connection, transaction);

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
            var connection = new SqlConnection(MsSqlTestHelper.ConnectionString);
            connection.Open();
            var transaction = connection.BeginTransaction();
            return new MsSqlFreakoutContext(connection, transaction);
        }

        void CommitAction(IFreakoutContext context)
        {
            var ctx = (MsSqlFreakoutContext)context;
            ctx.Transaction.Commit();
        }

        void DisposeAction(IFreakoutContext context)
        {
            var ctx = (MsSqlFreakoutContext)context;
            ctx.Transaction.Dispose();
            ctx.Connection.Dispose();
        }

        return new(provider, ContextFactory, CommitAction, DisposeAction);
    }
}