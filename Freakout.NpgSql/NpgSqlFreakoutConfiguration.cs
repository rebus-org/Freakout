using Freakout.NpgSql.Internals;
using Microsoft.Extensions.DependencyInjection;

namespace Freakout.NpgSql;

/// <summary>
/// Freakout configuration for using PostgreSQL as the store.
/// </summary>
/// <param name="connectionString">Configures the connection string to use to connect to Postgres</param>
public class NpgsqlFreakoutConfiguration(string connectionString) : FreakoutConfiguration
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

    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IOutboxCommandStore>(_ =>
        {
            var commandStore = new NpgsqlOutboxCommandStore(connectionString, TableName, SchemaName);

            if (AutomaticallyCreateSchema)
            {
                commandStore.CreateSchema();
            }

            return commandStore;
        });

        services.AddScoped<IOutbox, NpgsqlOutbox>();
    }
}