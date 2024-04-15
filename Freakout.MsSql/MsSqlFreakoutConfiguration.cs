using Freakout.Internals;
using Microsoft.Extensions.DependencyInjection;

namespace Freakout.MsSql;

/// <summary>
/// Freakout configuration for using Microsoft SQL Server as the store.
/// </summary>
/// <param name="connectionString">Configures the connection string to use to connect to SQL Server</param>
public class MsSqlFreakoutConfiguration(string connectionString) : FreakoutConfiguration
{
    /// <summary>
    /// Configures the store table schema name. Defaults to "dbo".
    /// </summary>
    public string SchemaName { get; set; } = "dbo";

    /// <summary>
    /// Configures the store table name. Defaults to "OutboxCommands".
    /// </summary>
    public string TableName { get; set; } = "OutboxCommands";

    /// <summary>
    /// Configures whether the schema should be created automatically
    /// </summary>
    public bool AutomaticallyCreateSchema { get; set; } = true;

    /// <summary>
    /// Configures the command processing batch size, i.e. how many outbox commands to fetch, execute, and complete per batch.
    /// </summary>
    public int CommandProcessingBatchSize { get; set; } = 1;

    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IFreakoutContextAccessor, AsyncLocalFreakoutContextAccessor>();
        
        services.AddSingleton<IOutboxCommandStore>(_ =>
        {
            var commandStore = new MsSqlOutboxCommandStore(connectionString, TableName, SchemaName, CommandProcessingBatchSize);

            if (AutomaticallyCreateSchema)
            {
                commandStore.CreateSchema();
            }

            return commandStore;
        });

        services.AddScoped<IOutbox, MsSqlOutbox>();
    }
}