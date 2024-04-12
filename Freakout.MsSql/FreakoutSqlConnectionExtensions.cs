using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Freakout.Internals;
using Freakout.Serialization;
using Microsoft.Data.SqlClient;
// ReSharper disable UseAwaitUsing

namespace Freakout.MsSql;

/// <summary>
/// Relevant extension methods for working with store commands in Microsoft SQL Server
/// </summary>
public static class FreakoutSqlConnectionExtensions
{
    /// <summary>
    /// Adds the given <paramref name="command"/> to the store as par of the SQL transaction. The command will be added to the store
    /// when the transaction is committed.
    /// </summary>
    public static async Task AddOutboxCommandAsync(this DbTransaction transaction, object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (command == null) throw new ArgumentNullException(nameof(command));

        var serializer = Globals.Get<ICommandSerializer>();
        var serializedCommand = serializer.Serialize(command);

        var payload = serializedCommand.Payload;
        var headersToUse = serializedCommand.Headers;

        headers?.InsertInto(headersToUse);

        await Insert(transaction, HeaderSerializer.SerializeToString(headersToUse), payload, cancellationToken);
    }

    static async Task Insert(DbTransaction transaction, string headers, byte[] bytes, CancellationToken cancellationToken)
    {
        var connection = transaction.Connection ?? throw new ArgumentException($"The {transaction} did not have a DbConnection on it!");

        using var cmd = connection.CreateCommand();

        cmd.Transaction = transaction;
        cmd.CommandText = "INSERT INTO [OutboxCommands] ([Id], [Time], [Headers], [Payload]) VALUES (@id, SYSDATETIMEOFFSET(), @headers, @payload)";
        cmd.Parameters.Add(new SqlParameter("id", Guid.NewGuid()));
        cmd.Parameters.Add(new SqlParameter("headers", headers));
        cmd.Parameters.Add(new SqlParameter("payload", bytes));

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}