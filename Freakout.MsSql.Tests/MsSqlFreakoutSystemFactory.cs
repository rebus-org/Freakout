using Freakout.Config;
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