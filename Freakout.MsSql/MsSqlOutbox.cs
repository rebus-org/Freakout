using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Nito.Disposables;

namespace Freakout.MsSql;

class MsSqlOutbox(string connectionString, string tableName, string schemaName) : IOutbox
{
    readonly string _selectQuery = $"SELECT TOP 100 *  FROM [{schemaName}].[{tableName}] WITH (ROWLOCK, UPDLOCK) WHERE [Completed] = 0 ORDER BY [Id]";

    public async Task<OutboxCommandBatch> GetPendingOutboxCommandsAsync(CancellationToken cancellationToken = default)
    {
        var disposables = new CollectionDisposable();
        var connection = new SqlConnection(connectionString);
        disposables.Add(connection);

        await connection.OpenAsync(cancellationToken);

        var transaction = connection.BeginTransaction();
        disposables.Add(transaction);

        try
        {
            using var command = connection.CreateCommand();

            command.CommandText = _selectQuery;
            command.Transaction = transaction;

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var outboxCommands = new List<MsSqlOutboxCommand>();

            while (await reader.ReadAsync(cancellationToken))
            {
                var id = (Guid)reader["Id"];
                var time = (DateTimeOffset)reader["Time"];
                var headers = JsonSerializer.Deserialize<Dictionary<string, string>>((string)reader["Headers"]);
                var payload = (byte[])reader["Payload"];

                outboxCommands.Add(new MsSqlOutboxCommand(id, time, headers, payload));
            }

            return new OutboxCommandBatch(
                outboxCommands: outboxCommands,
                completeAsync: token => CompleteCommandsAsync(connection, transaction, outboxCommands, token)
            );
        }
        catch (Exception exception)
        {
            disposables.Dispose();
            throw new ApplicationException("Could not get outbox tasks", exception);
        }
    }

    async Task CompleteCommandsAsync(SqlConnection connection, SqlTransaction transaction, List<MsSqlOutboxCommand> outboxCommands, CancellationToken cancellationToken)
    {
        var ids = string.Join(",", outboxCommands.Select(c => $"'{c.Id}'"));

        using var command = connection.CreateCommand();

        command.CommandText = $"UPDATE [{schemaName}].[{tableName}] SET [Completed] = @completed WHERE [Id] IN ({ids})";
        command.Transaction = transaction;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public void CreateSchema()
    {
        using var connection = new SqlConnection(connectionString);

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = $@"

IF NOT EXISTS (SELECT TOP 1 * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = '{tableName}' AND s.name = '{schemaName}') 
BEGIN
    CREATE TABLE [{schemaName}].[{tableName}] (
        [Id] UNIQUEIDENTIFIER,
        [Time] DATETIMEOFFSET(3) NOT NULL,
        [Headers] NVARCHAR(MAX),
        [Payload] VARBINARY(MAX),
        [Completed] BIT NOT NULL DEFAULT(0),

        PRIMARY KEY ([Id])
    )
END
";

        command.ExecuteNonQuery();
    }
}