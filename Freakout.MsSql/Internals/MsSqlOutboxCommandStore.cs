using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Freakout.Serialization;
using Microsoft.Data.SqlClient;
using Nito.Disposables;
// ReSharper disable AccessToDisposedClosure
// ReSharper disable UseAwaitUsing

namespace Freakout.MsSql.Internals;

class MsSqlOutboxCommandStore(string connectionString, string tableName, string schemaName, int processingBatchSize) : IOutboxCommandStore
{
    readonly string _selectQuery = $"SELECT TOP {processingBatchSize} * FROM [{schemaName}].[{tableName}] WITH (ROWLOCK, UPDLOCK, READPAST) WHERE [Completed] = 0 ORDER BY [Id]";

    public async Task<OutboxCommandBatch> GetPendingOutboxCommandsAsync(CancellationToken cancellationToken = default)
    {
        // Collect disposables in this one 👇 Remember to consider disposal in all possible exit paths from this method!!
        var disposables = new CollectionDisposable();

        var connection = new SqlConnection(connectionString);
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

            var outboxCommands = new List<PendingOutboxCommand>();

            while (await reader.ReadAsync(cancellationToken))
            {
                var id = (Guid)reader["Id"];
                var createdAt = (DateTimeOffset)reader["CreatedAt"];
                var headers = HeaderSerializer.DeserializeFromString((string)reader["Headers"]);
                var payload = (byte[])reader["Payload"];

                outboxCommands.Add(new PendingOutboxCommand(id, createdAt, headers, payload));
            }

            if (!outboxCommands.Any())
            {
                // EXIT - dispose
                disposables.Dispose();
                return OutboxCommandBatch.Empty;
            }

            // EXIT - dispose deferred to dispose callback
            return new OutboxCommandBatch(
                freakoutContext: new MsSqlFreakoutContext(connection, transaction),
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

    async Task CompleteAsync(SqlConnection connection, SqlTransaction transaction, List<PendingOutboxCommand> outboxCommands, CancellationToken cancellationToken)
    {
        var ids = string.Join(",", outboxCommands.Select(c => $"'{c.Id}'"));

        using var command = connection.CreateCommand();
        command.CommandText = $"UPDATE [{schemaName}].[{tableName}] SET [Completed] = 1, [ExecutedAt] = SYSDATETIMEOFFSET() WHERE [Id] IN ({ids})";
        command.Transaction = transaction;
        await command.ExecuteNonQueryAsync(cancellationToken);

        transaction.Commit();
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
        [CreatedAt] DATETIMEOFFSET(3) NOT NULL,
        [Headers] NVARCHAR(MAX),
        [Payload] VARBINARY(MAX),
        [Completed] BIT NOT NULL DEFAULT(0),
        [ExecutedAt] DATETIMEOFFSET(3) NULL,

        PRIMARY KEY ([Id])
    )
END

";

        command.ExecuteNonQuery();
    }
}