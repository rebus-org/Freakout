using Microsoft.Extensions.DependencyInjection;

namespace Freakout.MsSql;

/// <summary>
/// Freakout configuration for using Microsoft SQL Server as the store.
/// </summary>
/// <param name="connectionString">Configures the connection string to use to connect to SQL Server</param>
/// <param name="automaticallyCreateSchema">Specifies whether the store table should be automaticallly created if it doesn't exist</param>
/// <param name="processingBatchSize">Configures the command processing batch size. Defaults to 1</param>
public class MsSqlFreakoutConfiguration(string connectionString, bool automaticallyCreateSchema = true, int processingBatchSize = 1) : FreakoutConfiguration
{
    /// <summary>
    /// Configures the store table schema name. Defaults to "dbo".
    /// </summary>
    public string SchemaName { get; set; } = "dbo";

    /// <summary>
    /// Configures the store table name. Defaults to "OutboxCommands".
    /// </summary>
    public string TableName { get; set; } = "OutboxCommands";

    internal override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IOutboxCommandStore>(_ =>
        {
            var msSqlOutbox = new MsSqlOutboxCommandStore(connectionString, TableName, SchemaName, processingBatchSize);

            if (automaticallyCreateSchema)
            {
                msSqlOutbox.CreateSchema();
            }

            return msSqlOutbox;
        });
    }
}