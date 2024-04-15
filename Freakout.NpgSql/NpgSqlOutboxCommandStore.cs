using Freakout.Serialization;
using Nito.Disposables;
using Npgsql;

namespace Freakout.NpgSql;

public class NpgSqlOutboxCommandStore(string connectionString, string tableName, string schemaName, int commandProcessingBatchSize) : IOutboxCommandStore
{
    readonly string _selectQuery = $@"SELECT * FROM ""{schemaName}"".""{tableName}"" WHERE ""completed"" = FALSE ORDER BY ""id"" FOR UPDATE SKIP LOCKED LIMIT {commandProcessingBatchSize};";

    public async Task<OutboxCommandBatch> GetPendingOutboxCommandsAsync(CancellationToken cancellationToken = default)
    {
        // Collect disposables in this one 👇 Remember to consider disposal in all possible exit paths from this method!!
        var disposables = new CollectionDisposable();

        var connection = new NpgsqlConnection(connectionString);
        disposables.Add(connection);

        try
        {
            await connection.OpenAsync(cancellationToken);

            var transaction = connection.BeginTransaction();
            disposables.Add(transaction);

            using var command = connection.CreateCommand();

            command.CommandText = _selectQuery;
            command.Transaction = transaction;

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var outboxCommands = new List<NpgsqlOutboxCommand>();

            while (await reader.ReadAsync(cancellationToken))
            {
                var id = (Guid)reader["id"];
                var time = new DateTimeOffset((DateTime)reader["time"]);
                var headers = HeaderSerializer.DeserializeFromString((string)reader["headers"]);
                var payload = (byte[])reader["payload"];

                outboxCommands.Add(new NpgsqlOutboxCommand(id, time, headers, payload));
            }

            if (!outboxCommands.Any())
            {
                // EXIT - dispose
                disposables.Dispose();
                return OutboxCommandBatch.Empty;
            }

            // EXIT - dispose deferred to dispose callback
            return new OutboxCommandBatch(
                outboxCommands: outboxCommands,
                completeAsync: token => CompleteAsync(connection, transaction, outboxCommands, token),
                dispose: disposables.Dispose
            );
        }
        catch (Exception exception)
        {
            // EXIT - dispose
            disposables.Dispose();
            throw new ApplicationException("Could not get store tasks", exception);
        }
    }

    async Task CompleteAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, List<NpgsqlOutboxCommand> outboxCommands, CancellationToken cancellationToken)
    {
        var ids = string.Join(",", outboxCommands.Select(c => $"'{c.Id}'"));

        using var command = connection.CreateCommand();
        command.CommandText = $@"UPDATE ""{schemaName}"".""{tableName}"" SET ""completed"" = TRUE WHERE ""id"" IN ({ids});";
        command.Transaction = transaction;
        await command.ExecuteNonQueryAsync(cancellationToken);

        transaction.Commit();
    }

    public void CreateSchema()
    {
        using var connection = new NpgsqlConnection(connectionString);

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = $@"

CREATE TABLE IF NOT EXISTS {schemaName}.{tableName} (
    ""id"" UUID PRIMARY KEY,
    ""time"" TIMESTAMPTZ NOT NULL,
    ""headers"" JSONB,
    ""payload"" BYTEA,
    ""completed"" BOOLEAN NOT NULL DEFAULT FALSE
);

";

        command.ExecuteNonQuery();

    }
}