using Microsoft.Extensions.DependencyInjection;

namespace Freakout.NpgSql;

/// <summary>
/// Freakout configuration for using PostgreSQL as the store.
/// </summary>
/// <param name="connectionString">Configures the connection string to use to connect to Postgres</param>
public class NpgSqlFreakoutConfiguration(string connectionString) : FreakoutConfiguration
{
    /// <summary>
    /// Configures the store table schema name. Defaults to "public".
    /// </summary>
    public string SchemaName { get; set; } = "public";

    /// <summary>
    /// Configures the store table name. Defaults to "OutboxCommands".
    /// </summary>
    public string TableName { get; set; } = "outbox_commands";

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
        services.AddSingleton<IOutboxCommandStore>(_ =>
        {
            var commandStore = new NpgSqlOutboxCommandStore(connectionString, TableName, SchemaName, CommandProcessingBatchSize);

            if (AutomaticallyCreateSchema)
            {
                commandStore.CreateSchema();
            }

            return commandStore;
        });

        services.AddScoped<IOutbox>(_ => new NpgSqlOutbox(connectionString));
    }
}