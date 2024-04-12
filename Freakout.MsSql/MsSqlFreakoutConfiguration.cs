using Microsoft.Extensions.DependencyInjection;

namespace Freakout.MsSql;

public class MsSqlFreakoutConfiguration(string connectionString, bool automaticallyCreateSchema = true, int processingBatchSize = 1) : FreakoutConfiguration
{
    public string SchemaName { get; set; } = "dbo";

    public string TableName { get; set; } = "OutboxCommands";

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IOutbox>(_ =>
        {
            var msSqlOutbox = new MsSqlOutbox(connectionString, TableName, SchemaName, processingBatchSize);

            if (automaticallyCreateSchema)
            {
                msSqlOutbox.CreateSchema();
            }

            return msSqlOutbox;
        });
    }
}